using System.IO;


namespace WalkieDohi.Core
{
    /// <summary>
    /// System.IO.File 클래스를 래핑하려했지만 사용자 정의 메서드만 포함된 유틸리티 클래스입니다.
    /// </summary>
    public static class ExtendFile
    {
        /// <summary>
        /// 지정한 경로에 파일이 존재하지 않는 경우 true를 반환합니다.
        /// </summary>
        /// <param name="path">path 파라미터입니다.</param>
        /// <returns>파일존재하지않음 true 존재 false</returns>
        public static bool UnExists(string path) => !File.Exists(path);
    }
}
