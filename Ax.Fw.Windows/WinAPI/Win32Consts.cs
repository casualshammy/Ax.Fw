namespace Ax.Fw.Windows.WinAPI
{
    public static class Win32Consts
    {
        public const uint WM_NULL = 0x0;
        public const uint WM_QUERYENDSESSION = 0x11;
        public const int WM_ENDSESSION = 0x16;
        public const uint WM_KEYDOWN = 0x100;
        public const uint WM_KEYUP = 0x101;
        public const uint WM_CHAR = 0x102;
        public const uint WM_BM_CLICK = 0xF5;
        public const uint WM_RBUTTONDOWN = 0x204; //Right mousebutton down
        public const uint WM_RBUTTONUP = 0x205;   //Right mousebutton up
        public const uint WS_CAPTION = 0xC00000;
        public const uint WS_THICKFRAME = 0x40000;
        public const int WS_MINIMIZE = 0x20000000;
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const uint WS_EX_LAYERED = 0x80000;
        public const uint WS_EX_TRANSPARENT = 0x20;
        public const uint LWA_ALPHA = 0x2;
        public const int SND_ALIAS = 65536;
        public const int SND_NODEFAULT = 2;

        public const int PAGE_EXECUTE = 0x10;
        public const int PAGE_EXECUTE_READ = 0x20;
        public const int PAGE_EXECUTE_READWRITE = 0x40;
        public const int PAGE_EXECUTE_WRITECOPY = 0x80;
        public const int PAGE_NOACCESS = 0x01;
        public const int PAGE_READONLY = 0x02;
        public const int PAGE_READWRITE = 0x04;
        public const int PAGE_WRITECOPY = 0x08;
        public const int PAGE_TARGETS_INVALID = 0x40000000;
        public const int PAGE_TARGETS_NO_UPDATE = 0x40000000;
    }
}