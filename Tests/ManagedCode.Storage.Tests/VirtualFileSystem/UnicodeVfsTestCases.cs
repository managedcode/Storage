using System.Collections.Generic;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public static class UnicodeVfsTestCases
{
    public static IEnumerable<object[]> FolderScenarios => new[]
    {
        new object[] { "Українська-папка", "лист-привіт", "Привіт з Києва!" },
        new object[] { "中文目錄", "測試文件", "雲端中的內容" },
        new object[] { "日本語ディレクトリ", "テストファイル", "東京からこんにちは" },
        new object[] { "한국어_폴더", "테스트-파일", "부산에서 안녕하세요" },
        new object[] { "emoji📁", "😀-файл", "multi🌐lingual content" }
    };
}
