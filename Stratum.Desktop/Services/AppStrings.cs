namespace Stratum.Desktop.Services
{
    public static class AppStrings
    {
        private static string _code = "zh";

        public static bool IsEnglish => _code == "en";

        public static void SetLanguage(string code)
        {
            _code = code;
        }

        private static string Get(string zh, string en)
        {
            return IsEnglish ? en : zh;
        }

        public static string TypeTotp => Get("TOTP (基于时间)", "TOTP (time-based)");

        public static string TypeHotp => Get("HOTP (基于计数)", "HOTP (counter-based)");

        public static string TypeSteam => Get("Steam", "Steam");

        public static string TypeMotp => Get("mOTP (Mobile-OTP)", "mOTP (Mobile-OTP)");

        public static string TypeYandex => Get("Yandex", "Yandex");

        public static string All => Get("全部", "All");

        public static string Uncategorized => Get("未分类", "Uncategorized");

        public static string AllFiles => Get("全部文件", "All files");

        public static string AutoDetect => Get("自动检测", "Auto detect");

        public static string ImportNeedsPassword => Get("已识别加密备份,请输入密码",
            "Encrypted backup detected, enter the password");

        public static string NoCategory => Get("无分类", "No category");

        public static string PinRequired => Get("此类型需要 PIN", "PIN is required for this type");

        public static string ScanOk => Get("识别成功", "Recognized");

        public static string ScanFailed => Get("识别失败: ", "Recognition failed: ");

        public static string ScanNone => Get("未识别到二维码", "No QR code found");

        public static string BackupExported => Get("备份已导出", "Backup exported");

        public static string ExportFailed => Get("导出失败: ", "Export failed: ");

        public static string ImportComplete => Get("导入完成", "Import complete");

        public static string ImportFailed => Get("导入失败: ", "Import failed: ");

        public static string RestoreComplete => Get("备份恢复完成", "Backup restored");

        public static string DecryptFailed => Get("无法解密备份:密码错误或文件已损坏",
            "Unable to decrypt backup: wrong password or corrupt file");

        public static string QrInvalid => Get("未识别到有效的 otpauth 二维码", "No valid otpauth QR code found");

        public static string QrFailed => Get("扫码失败: ", "Scan failed: ");

        public static string WrongPassword => Get("密码错误,请重试", "Wrong password, please retry");

        public static string ImportCancelledPassword => Get("导入已取消:需要密码", "Import cancelled: password required");

        public static string ImportCancelled => Get("导入已取消", "Import cancelled");

        public static string UnknownFormatPick => Get("无法识别文件格式,请选择具体格式",
            "Unable to detect the file format, please choose one");

        public static string IconPackImported => Get("图标包已导入: ", "Icon pack imported: ");

        public static string IconPackFailed => Get("导入图标包失败: ", "Icon pack import failed: ");

        public static string CategoryFailed => Get("新建分类失败: ", "Failed to create category: ");

        public static string RenameFailed => Get("重命名失败: ", "Rename failed: ");

        public static string DeleteAccountTitle => Get("删除账户", "Delete account");

        public static string DeleteCategoryTitle => Get("删除分类", "Delete category");

        public static string BackupPasswordTitle => Get("输入备份密码", "Enter backup password");

        public static string BackupEncryptedMsg => Get("此备份已加密,请输入密码", "This backup is encrypted, enter the password");

        public static string BackupSetPasswordMsg => Get("设置备份密码(留空则不加密)",
            "Set a backup password (empty = no encryption)");

        public static string EnterPasswordTitle => Get("输入密码", "Enter password");

        public static string PasswordRequiredMsg => Get("此格式的备份需要密码解密", "This format requires a password to decrypt");

        public static string MaybePasswordMsg => Get("此备份可能已加密,请输入密码(留空重试)",
            "This backup may be encrypted, enter the password (empty to retry)");

        public static string ExportBackupTitle => Get("导出备份", "Export backup");

        public static string NewCategoryTitle => Get("新建分类", "New category");

        public static string CategoryNameMsg => Get("输入分类名称", "Enter category name");

        public static string CategoryNamePh => Get("分类名称", "Category name");

        public static string RenameCategoryTitle => Get("重命名分类", "Rename category");

        public static string NewCategoryNameMsg => Get("输入新的分类名称", "Enter new category name");

        public static string AddTitle => Get("添加账户", "Add account");

        public static string EditTitle => Get("编辑账户", "Edit account");

        public static string AddButton => Get("添加", "Add");

        public static string SaveButton => Get("保存", "Save");

        public static string SelectQrImage => Get("选择二维码图片", "Choose QR code image");

        public static string SelectImage => Get("选择图片", "Choose image");

        public static string SelectBackupFile => Get("选择备份文件", "Choose backup file");

        public static string SelectIconPack => Get("选择图标包", "Choose icon pack");

        public static string SaveBackupTitle => Get("保存备份", "Save backup");

        public static string IconPackFileType => Get("图标包", "Icon pack");

        public static string BackupFileType => Get("备份", "Backup");

        public static string DeleteAccountFmt(string name)
        {
            return Get($"确定删除「{name}」吗?", $"Delete \"{name}\"?");
        }

        public static string DeleteCategoryFmt(string name)
        {
            return Get($"确定删除分类「{name}」吗?", $"Delete category \"{name}\"?");
        }

        public static string ImportCompleteWithFailures(int count)
        {
            return Get($"导入完成,{count} 个条目失败", $"Import complete, {count} entries failed");
        }
    }
}
