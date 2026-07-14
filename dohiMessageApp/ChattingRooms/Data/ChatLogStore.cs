using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Groups.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.IO;

namespace WalkieDohi.ChattingRooms.Data
{
    public static class ChatLogStore
    {
        public const int DefaultPageSize = 200;

        private static readonly object InitLock = new object();
        private static bool _initialized;

        private static string DatabasePath => DirectoryManager.GetAppDataDirectoryCombineFileName("ChatMessages.db");

        private static string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public static string GetSingleRoomKey(string targetIp)
        {
            return targetIp ?? "";
        }

        public static string GetGroupRoomKey(GroupEntity group)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.Key))
                return "";

            return $"group_{DirectoryManager.MakeSafeFileName(group.Key)}";
        }

        public static string GetLegacyRoomDirectory(string roomKey)
        {
            return Path.Combine(DirectoryManager.GetAppDataDirectoryCombineFileName("ChatLogs"), roomKey);
        }

        public static void SaveMessages(string roomKey, IEnumerable<ChatMessage> messages)
        {
            var list = messages?
                .Where(m => m != null)
                .OrderBy(m => m.Timestamp)
                .ToList() ?? new List<ChatMessage>();

            if (string.IsNullOrWhiteSpace(roomKey) || list.Count == 0)
                return;

            EnsureSchema();

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            using (var updateCommand = connection.CreateCommand())
            using (var insertCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText =
                    @"UPDATE chat_messages
                         SET message_type = @message_type,
                             direction = @direction,
                             sender = @sender,
                             sender_ip = @sender_ip,
                             content = @content,
                             file_name = @file_name,
                             content_path = @content_path,
                             is_failed = @is_failed,
                             failure_text = @failure_text,
                             failure_detail = @failure_detail,
                             timestamp_ticks = @timestamp_ticks,
                             timestamp_text = @timestamp_text,
                             saved_at_utc = @saved_at_utc
                       WHERE room_key = @room_key
                         AND message_id = @message_id
                         AND message_id IS NOT NULL
                         AND message_id <> '';";
                AddMessageParameters(updateCommand);

                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    @"INSERT OR IGNORE INTO chat_messages
                      (room_key, message_id, message_type, direction, sender, sender_ip, content, file_name, content_path,
                       is_failed, failure_text, failure_detail, timestamp_ticks, timestamp_text, saved_at_utc)
                      VALUES
                      (@room_key, @message_id, @message_type, @direction, @sender, @sender_ip, @content, @file_name, @content_path,
                       @is_failed, @failure_text, @failure_detail, @timestamp_ticks, @timestamp_text, @saved_at_utc);";
                AddMessageParameters(insertCommand);

                foreach (var message in list)
                {
                    var entity = message.ToEntity();
                    SetMessageParameterValues(updateCommand, roomKey, message, entity);
                    var updated = updateCommand.ExecuteNonQuery();

                    if (updated == 0)
                    {
                        SetMessageParameterValues(insertCommand, roomKey, message, entity);
                        insertCommand.ExecuteNonQuery();
                    }

                    message.IsSaved = true;
                }

                transaction.Commit();
            }
        }

        public static List<ChatMessage> LoadMessages(string roomKey, int offset, int limit)
        {
            if (string.IsNullOrWhiteSpace(roomKey))
                return new List<ChatMessage>();

            EnsureSchema();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT message_id, message_type, direction, sender, sender_ip, content, file_name, content_path,
                             is_failed, failure_text, failure_detail, timestamp_ticks
                        FROM chat_messages
                       WHERE room_key = @room_key
                       ORDER BY timestamp_ticks DESC, id DESC
                       LIMIT @limit OFFSET @offset;";

                command.Parameters.AddWithValue("@room_key", roomKey);
                command.Parameters.AddWithValue("@limit", limit);
                command.Parameters.AddWithValue("@offset", Math.Max(offset, 0));

                var messages = new List<ChatMessage>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var message = ReadMessage(reader);
                        if (message != null)
                            messages.Add(message);
                    }
                }

                messages.Reverse();
                return messages;
            }
        }

        public static void DeleteRoom(string roomKey)
        {
            if (string.IsNullOrWhiteSpace(roomKey))
                return;

            EnsureSchema();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM chat_messages WHERE room_key = @room_key;";
                command.Parameters.AddWithValue("@room_key", roomKey);
                command.ExecuteNonQuery();
            }
        }

        public static bool HasMessage(string roomKey, string messageId)
        {
            if (string.IsNullOrWhiteSpace(roomKey) || string.IsNullOrWhiteSpace(messageId))
                return false;

            EnsureSchema();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM chat_messages WHERE room_key = @room_key AND message_id = @message_id;";
                command.Parameters.AddWithValue("@room_key", roomKey);
                command.Parameters.AddWithValue("@message_id", messageId);
                return Convert.ToInt64(command.ExecuteScalar()) > 0;
            }
        }

        public static void MigrateLegacyFilesIfNeeded(string roomKey)
        {
            if (string.IsNullOrWhiteSpace(roomKey) || CountMessages(roomKey) > 0)
                return;

            var legacyDirectory = GetLegacyRoomDirectory(roomKey);
            if (!Directory.Exists(legacyDirectory))
                return;

            var files = Directory.EnumerateFiles(legacyDirectory)
                .Where(f => System.Text.RegularExpressions.Regex.IsMatch(
                    Path.GetFileName(f),
                    @"^chat_.*\.(json|dohi)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var entities = JsonUtil.Deserialize<List<MessageEntity>>(json) ?? new List<MessageEntity>();
                    var messages = new List<ChatMessage>();
                    foreach (var entity in entities)
                    {
                        try
                        {
                            var message = entity.ToChatMessage(true);
                            if (message != null)
                                messages.Add(message);
                        }
                        catch
                        {
                            // 손상된 메시지 하나 때문에 같은 파일의 나머지 로그를 버리지 않습니다.
                        }
                    }

                    SaveMessages(roomKey, messages);
                }
                catch
                {
                    // 손상된 과거 로그 하나 때문에 채팅방 전체 로딩이 막히지 않게 넘깁니다.
                }
            }
        }

        private static long CountMessages(string roomKey)
        {
            EnsureSchema();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM chat_messages WHERE room_key = @room_key;";
                command.Parameters.AddWithValue("@room_key", roomKey);
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        private static ChatMessage ReadMessage(SQLiteDataReader reader)
        {
            MessageType type;
            if (!Enum.TryParse(ReadString(reader, "message_type"), out type))
                type = MessageType.Text;

            MessageDirection direction;
            if (!Enum.TryParse(ReadString(reader, "direction"), out direction))
                direction = MessageDirection.Receive;

            var timestampTicks = ReadInt64(reader, "timestamp_ticks");
            var timestamp = timestampTicks > 0 ? new DateTime(timestampTicks) : DateTime.Now;

            var entity = new MessageEntity
            {
                MessageId = ReadString(reader, "message_id"),
                Type = type,
                Sender = ReadString(reader, "sender"),
                SenderIp = ReadString(reader, "sender_ip"),
                Content = ReadString(reader, "content"),
                FileName = ReadString(reader, "file_name"),
                ContentPath = ReadString(reader, "content_path"),
                IsFailed = ReadInt32(reader, "is_failed") == 1,
                FailureText = ReadString(reader, "failure_text"),
                FailureDetail = ReadString(reader, "failure_detail"),
                Timestamp = timestamp
            };

            ChatMessage message = null;
            if (entity.CheckMessageTypeText)
            {
                message = new TextMessage(entity.Sender, entity.Content, direction, entity.Timestamp, entity.SenderIp, null, true);
            }
            else if (entity.CheckMessageTypeImage)
            {
                message = new ImageMessage(entity.Sender, entity.FileName, entity.Content, entity.ContentPath, direction, entity.Timestamp, entity.SenderIp, null, true);
            }
            else if (entity.CheckMessageTypeFile)
            {
                message = new FileMessage(entity.Sender, entity.FileName, entity.ContentPath, direction, entity.Timestamp, entity.SenderIp, null, true);
            }

            if (message != null)
            {
                message.MessageId = entity.MessageId;
                message.IsFailed = entity.IsFailed;
                message.FailureText = entity.FailureText;
                message.FailureDetail = entity.FailureDetail;
                message.IsSaved = true;
            }

            return message;
        }

        private static void EnsureSchema()
        {
            if (_initialized)
                return;

            lock (InitLock)
            {
                if (_initialized)
                    return;

                using (var connection = OpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA journal_mode=WAL;";
                    command.ExecuteNonQuery();

                    command.CommandText =
                        @"CREATE TABLE IF NOT EXISTS chat_messages (
                              id INTEGER PRIMARY KEY AUTOINCREMENT,
                              room_key TEXT NOT NULL,
                              message_id TEXT,
                              message_type TEXT NOT NULL,
                              direction TEXT NOT NULL,
                              sender TEXT,
                              sender_ip TEXT,
                              content TEXT,
                              file_name TEXT,
                              content_path TEXT,
                              is_failed INTEGER NOT NULL DEFAULT 0,
                              failure_text TEXT,
                              failure_detail TEXT,
                              timestamp_ticks INTEGER NOT NULL,
                              timestamp_text TEXT NOT NULL,
                              saved_at_utc TEXT NOT NULL
                          );";
                    command.ExecuteNonQuery();

                    EnsureColumn(connection, "chat_messages", "message_id", "TEXT");

                    command.CommandText =
                        @"CREATE INDEX IF NOT EXISTS idx_chat_messages_room_time
                              ON chat_messages(room_key, timestamp_ticks, id);";
                    command.ExecuteNonQuery();

                    command.CommandText =
                        @"CREATE UNIQUE INDEX IF NOT EXISTS ux_chat_messages_room_message_id
                              ON chat_messages(room_key, message_id)
                           WHERE message_id IS NOT NULL AND message_id <> '';";
                    command.ExecuteNonQuery();
                }

                _initialized = true;
            }
        }

        private static SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private static void EnsureColumn(SQLiteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName});";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(Convert.ToString(reader["name"]), columnName, StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }

                command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
                command.ExecuteNonQuery();
            }
        }

        private static void AddMessageParameters(SQLiteCommand command)
        {
            AddParameter(command, "@room_key");
            AddParameter(command, "@message_id");
            AddParameter(command, "@message_type");
            AddParameter(command, "@direction");
            AddParameter(command, "@sender");
            AddParameter(command, "@sender_ip");
            AddParameter(command, "@content");
            AddParameter(command, "@file_name");
            AddParameter(command, "@content_path");
            AddParameter(command, "@is_failed");
            AddParameter(command, "@failure_text");
            AddParameter(command, "@failure_detail");
            AddParameter(command, "@timestamp_ticks");
            AddParameter(command, "@timestamp_text");
            AddParameter(command, "@saved_at_utc");
        }

        private static void SetMessageParameterValues(SQLiteCommand command, string roomKey, ChatMessage message, MessageEntity entity)
        {
            SetValue(command, "@room_key", roomKey);
            SetValue(command, "@message_id", entity.MessageId);
            SetValue(command, "@message_type", entity.Type.ToString());
            SetValue(command, "@direction", message.Direction.ToString());
            SetValue(command, "@sender", entity.Sender);
            SetValue(command, "@sender_ip", entity.SenderIp);
            SetValue(command, "@content", entity.Content);
            SetValue(command, "@file_name", entity.FileName);
            SetValue(command, "@content_path", entity.ContentPath);
            SetValue(command, "@is_failed", entity.IsFailed ? 1 : 0);
            SetValue(command, "@failure_text", entity.FailureText);
            SetValue(command, "@failure_detail", entity.FailureDetail);
            SetValue(command, "@timestamp_ticks", entity.Timestamp.Ticks);
            SetValue(command, "@timestamp_text", entity.Timestamp.ToString("o"));
            SetValue(command, "@saved_at_utc", DateTime.UtcNow.ToString("o"));
        }

        private static SQLiteParameter AddParameter(SQLiteCommand command, string name)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);
            return parameter;
        }

        private static void SetValue(SQLiteCommand command, string name, object value)
        {
            command.Parameters[name].Value = value ?? DBNull.Value;
        }

        private static string ReadString(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? "" : Convert.ToString(value);
        }

        private static int ReadInt32(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static long ReadInt64(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt64(value);
        }
    }
}
