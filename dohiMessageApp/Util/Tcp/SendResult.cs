using System;
using System.Collections.Generic;
using System.Linq;

namespace WalkieDohi.Util.Tcp
{
    public class SendFailure
    {
        public string TargetIp { get; private set; }
        public string TargetName { get; private set; }
        public string ErrorMessage { get; private set; }

        public SendFailure(string targetIp, string targetName, string errorMessage)
        {
            TargetIp = targetIp ?? "";
            TargetName = targetName ?? "";
            ErrorMessage = errorMessage ?? "알 수 없는 전송 오류";
        }

        public string DisplayTarget
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TargetIp))
                {
                    return string.IsNullOrWhiteSpace(TargetName) ? "대상 없음" : TargetName;
                }

                return string.IsNullOrWhiteSpace(TargetName)
                    ? TargetIp
                    : $"{TargetName} ({TargetIp})";
            }
        }

        public string ToDetailLine()
        {
            return $"{DisplayTarget}: {ErrorMessage}";
        }
    }

    public class SendResult
    {
        public bool Succeeded { get; private set; }
        public string TargetIp { get; private set; }
        public string ErrorMessage { get; private set; }
        public string FailureText { get; private set; }
        public string FailureDetail { get; private set; }
        public int TotalCount { get; private set; }
        public int FailedCount { get; private set; }
        public List<SendFailure> Failures { get; private set; }

        public bool Failed
        {
            get { return !Succeeded; }
        }

        private SendResult(
            bool succeeded,
            string targetIp,
            string errorMessage,
            string failureText,
            string failureDetail,
            int totalCount,
            List<SendFailure> failures)
        {
            Succeeded = succeeded;
            TargetIp = targetIp;
            ErrorMessage = errorMessage;
            FailureText = failureText;
            FailureDetail = failureDetail;
            TotalCount = totalCount;
            Failures = failures ?? new List<SendFailure>();
            FailedCount = Failures.Count;
        }

        public static SendResult Success(string targetIp = "")
        {
            return new SendResult(true, targetIp, "", "", "", string.IsNullOrWhiteSpace(targetIp) ? 0 : 1, new List<SendFailure>());
        }

        public static SendResult Fail(string targetIp, string errorMessage)
        {
            var failure = new SendFailure(targetIp, "", errorMessage);
            var detail = failure.ToDetailLine();
            return new SendResult(false, targetIp, failure.ErrorMessage, "전송 실패", detail, 1, new List<SendFailure> { failure });
        }

        public static SendResult Aggregate(IEnumerable<SendResult> results, Func<string, string> targetNameResolver = null)
        {
            var list = results == null ? new List<SendResult>() : results.ToList();
            if (list.Count == 0 || list.All(r => r.Succeeded))
            {
                return Success();
            }

            var failures = list
                .Where(r => r.Failed)
                .SelectMany(r => BuildFailures(r, targetNameResolver))
                .ToList();

            var failureText = failures.Count >= list.Count ? "전체 실패" : "일부 실패";
            var detail = string.Join(Environment.NewLine, failures.Select(f => f.ToDetailLine()));

            return new SendResult(false, "", detail, failureText, detail, list.Count, failures);
        }

        private static IEnumerable<SendFailure> BuildFailures(SendResult result, Func<string, string> targetNameResolver)
        {
            if (result.Failures != null && result.Failures.Count > 0)
            {
                foreach (var failure in result.Failures)
                {
                    yield return new SendFailure(
                        failure.TargetIp,
                        ResolveTargetName(failure.TargetIp, failure.TargetName, targetNameResolver),
                        failure.ErrorMessage);
                }

                yield break;
            }

            yield return new SendFailure(
                result.TargetIp,
                ResolveTargetName(result.TargetIp, "", targetNameResolver),
                result.ErrorMessage);
        }

        private static string ResolveTargetName(string targetIp, string currentName, Func<string, string> targetNameResolver)
        {
            if (!string.IsNullOrWhiteSpace(currentName) || targetNameResolver == null)
            {
                return currentName;
            }

            return targetNameResolver(targetIp) ?? "";
        }
    }
}
