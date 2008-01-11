#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#ifndef _WIN32
#include <unistd.h>
#else
#define R_OK	4
#define W_OK	2
#endif

#include "glk.h"
#include "garglk.h"
#include "garversion.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commdlg.h>
#include <shellapi.h>

static char *argv0;

#define ID_ABOUT	0x1000
#define ID_CONFIG	0x1001
#define ID_TOGSCR	0x1002

static HWND hwndview, hwndframe;
static HDC hdc;
static BITMAPINFO *dibinf;
static LRESULT CALLBACK frameproc(HWND, UINT, WPARAM, LPARAM);
static LRESULT CALLBACK viewproc(HWND, UINT, WPARAM, LPARAM);

static int timeouts = 0;

void glk_request_timer_events(glui32 millisecs)
{
    if (millisecs)
	SetTimer(hwndframe, 1, millisecs, NULL);
    else
	KillTimer(hwndframe, 1);
}

void onabout(void)
{
    char txt[512];

    if (gli_program_info[0])
    {
	sprintf(txt,
		"Gargoyle by Tor Andersson   \n"
		"Build %s\n"
		"\n"
		"%s", VERSION, gli_program_info);
    }
    else
    {
	sprintf(txt,
		"Gargoyle by Tor Andersson   \n"
		"Build %s\n"
		"\n"
		"Interpeter: %s\n", VERSION, gli_program_name);
    }

    MessageBoxA(hwndframe, txt, " About", MB_OK);
}

void onconfig(void)
{
    char buf[256];
    strcpy(buf, argv0);
    if (strrchr(buf, '\\')) strrchr(buf, '\\')[1] = 0;
    if (strrchr(buf, '/')) strrchr(buf, '/')[1] = 0;
    strcat(buf, "garglk.ini");

    if (access(buf, R_OK))
    {
	char msg[1024];
	sprintf(msg, "There was no configuration file:    \n\n    %s    \n", buf);
	MessageBoxA(hwndframe, msg, " Configure", MB_ICONERROR);
    }
    else
	ShellExecute(hwndframe, "open", buf, 0, 0, SW_SHOWNORMAL);
}

void winabort(const char *fmt, ...)
{
    va_list ap;
    char buf[256];
    va_start(ap, fmt);
    vsprintf(buf, fmt, ap);
    va_end(ap);
    MessageBoxA(NULL, buf, " Fatal error", MB_ICONERROR);
    abort();
}

void winopenfile(char *prompt, char *buf, int len, char *filter)
{
    OPENFILENAME ofn;
    memset(&ofn, 0, sizeof(OPENFILENAME));
    ofn.lStructSize = sizeof(OPENFILENAME);
    ofn.hwndOwner = hwndframe;
    ofn.lpstrFile = buf;
    ofn.nMaxFile = len;
    ofn.lpstrInitialDir = NULL;
    ofn.lpstrTitle = prompt;
	ofn.lpstrFilter = filter;
	ofn.nFilterIndex = 1;
    ofn.Flags = OFN_FILEMUSTEXIST|OFN_HIDEREADONLY;

    if (!GetOpenFileName(&ofn))
	strcpy(buf, "");
}

void winsavefile(char *prompt, char *buf, int len, char *filter)
{
    OPENFILENAME ofn;
    memset(&ofn, 0, sizeof(OPENFILENAME));
    ofn.lStructSize = sizeof(OPENFILENAME);
    ofn.hwndOwner = hwndframe;
    ofn.lpstrFile = buf;
    ofn.nMaxFile = len;
    ofn.lpstrInitialDir = NULL;
    ofn.lpstrTitle = prompt;
	ofn.lpstrFilter = filter;
	ofn.nFilterIndex = 1;
    ofn.Flags = OFN_OVERWRITEPROMPT;

    if (!GetSaveFileName(&ofn))
	strcpy(buf, "");
}

void wininit(int *argc, char **argv)
{
    WNDCLASS wc;

    argv0 = argv[0];

    /* Create and register frame window class */
    wc.style = 0;
    wc.lpfnWndProc = frameproc;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;
    wc.hInstance = GetModuleHandle(NULL); 
    wc.hIcon = LoadIcon(wc.hInstance, "IDI_GAPP");
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = 0;
    wc.lpszMenuName = 0;
    wc.lpszClassName = "XxFrame";
    RegisterClass(&wc);

    /* Create and register view window class */
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = viewproc;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;
    wc.hInstance = GetModuleHandle(NULL);
    wc.hIcon = 0;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = 0;
    wc.lpszMenuName = 0;
    wc.lpszClassName = "XxView";
    RegisterClass(&wc);

    /* Init DIB info for buffer */
    dibinf = malloc(sizeof(BITMAPINFO) + 12);
    dibinf->bmiHeader.biSize = sizeof(dibinf->bmiHeader);
    dibinf->bmiHeader.biPlanes = 1;
    dibinf->bmiHeader.biBitCount = 24;
    dibinf->bmiHeader.biCompression = BI_RGB;
    dibinf->bmiHeader.biXPelsPerMeter = 2834;
    dibinf->bmiHeader.biYPelsPerMeter = 2834;
    dibinf->bmiHeader.biClrUsed = 0;
    dibinf->bmiHeader.biClrImportant = 0;
    dibinf->bmiHeader.biClrUsed = 0;
}

void winopen()
{
    HMENU menu;

    int sizew = gli_wmarginx * 2 + gli_cellw * gli_cols;
    int sizeh = gli_wmarginy * 2 + gli_cellh * gli_rows;

    sizew += GetSystemMetrics(SM_CXFRAME) * 2;
    sizeh += GetSystemMetrics(SM_CYFRAME) * 2;
    sizeh += GetSystemMetrics(SM_CYCAPTION);

    hwndframe = CreateWindow("XxFrame",
	    NULL, // window caption
	    WS_CAPTION|WS_THICKFRAME|
		WS_SYSMENU|WS_MAXIMIZEBOX|WS_MINIMIZEBOX|
		WS_CLIPCHILDREN,
	    CW_USEDEFAULT, // initial x position
	    CW_USEDEFAULT, // initial y position
	    sizew, // initial x size
	    sizeh, // initial y size
	    NULL, // parent window handle
	    NULL, // window menu handle
	    0, //hInstance, // program instance handle
	    NULL); // creation parameters

    hwndview = CreateWindow("XxView",
	    NULL,
	    WS_VISIBLE | WS_CHILD,
	    CW_USEDEFAULT, CW_USEDEFAULT,
	    CW_USEDEFAULT, CW_USEDEFAULT,
	    hwndframe,
	    NULL, NULL, 0);

    hdc = NULL;

    menu = GetSystemMenu(hwndframe, 0);
    AppendMenu(menu, MF_SEPARATOR, 0, NULL);
    AppendMenu(menu, MF_STRING, ID_ABOUT, "About Gargoyle...");
    AppendMenu(menu, MF_STRING, ID_CONFIG, "Options...");
    // AppendMenu(menu, MF_STRING, ID_TOGSCR, "Toggle scrollbar");

    wintitle();

    ShowWindow(hwndframe, SW_SHOW);
}

void wintitle(void)
{
    char buf[256];
    if (strlen(gli_story_name))
	sprintf(buf, "%s - %s", gli_story_name, gli_program_name);
    else
	sprintf(buf, "%s", gli_program_name);
    SetWindowTextA(hwndframe, buf);

	if (strcmp(gli_program_name, "Unknown"))
		sprintf(buf, "About Gargoyle / %s...", gli_program_name);
	else
		strcpy(buf, "About Gargoyle...");

	ModifyMenu(GetSystemMenu(hwndframe, 0), ID_ABOUT, MF_BYCOMMAND | MF_STRING, ID_ABOUT, buf);
	DrawMenuBar(hwndframe);
}

static void winblit(RECT r)
{
    int x0 = r.left;
    int y0 = r.top;
    int x1 = r.right;
    int y1 = r.bottom;

    dibinf->bmiHeader.biWidth = gli_image_w;
    dibinf->bmiHeader.biHeight = -gli_image_h;
    dibinf->bmiHeader.biSizeImage = gli_image_h * gli_image_s;

    SetDIBitsToDevice(hdc,
	    x0, /* destx */
	    y0, /* desty */
	    x1 - x0, /* destw */
	    y1 - y0, /* desth */
	    x0, /* srcx */
	    gli_image_h - y1, /* srcy */
	    0, /* startscan */
	    gli_image_h, /* numscans */
	    gli_image_rgb, /* pBits */
	    dibinf, /* pInfo */
	    DIB_RGB_COLORS /* color use flag */
		     );
}

void winrepaint(int x0, int y0, int x1, int y1)
{
    RECT wr;
	wr.left = x0; wr.right = x1;
	wr.top = y0; wr.bottom = y1;
    InvalidateRect(hwndview, &wr, 1); // 0);
}

void winloop(void)
{
    MSG msg;
    int i;

    i = GetMessage(&msg, NULL, 0, 0);
    if (i < 0)
	exit(1);
    if (i > 0)
    {
	TranslateMessage(&msg);
	DispatchMessage(&msg);
    }
}

void winpoll(void)
{
    MSG msg;
    int i;

    i = PeekMessage(&msg, NULL, 0, 0, PM_REMOVE);
    if (i)
    {
	TranslateMessage(&msg);
	DispatchMessage(&msg);
    }
}

void gli_select(event_t *event, int block)
{
    MSG msg;

    gli_curevent = event;
    gli_event_clearevent(event);

    gli_input_guess_focus();

    if (block)
    {
	while (gli_curevent->type == evtype_None && !timeouts)
	{
	    int code = GetMessage(&msg, NULL, 0, 0);
	    if (code < 0)
		exit(1);
	    if (code > 0)
	    {
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	    }
	}
    }

    else
    {
	while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE) > 0 && !timeouts)
	{
	    TranslateMessage(&msg);
	    DispatchMessage(&msg);
	}
    }

    if (gli_curevent->type == evtype_None && timeouts)
    {
	gli_event_store(evtype_Timer, NULL, 0, 0);
	timeouts = 0;
    }

    gli_curevent = NULL;
}

void winresize(void)
{
    int xw, xh;
    int w, h;

    xw = gli_wmarginx * 2;
    xh = gli_wmarginy * 2;
    gli_calc_padding(gli_rootwin, &xw, &xh);
    xw += GetSystemMetrics(SM_CXFRAME) * 2;
    xh += GetSystemMetrics(SM_CYFRAME) * 2;
    xh += GetSystemMetrics(SM_CYCAPTION);

    w = (gli_cols * gli_cellw) + xw;
    h = (gli_rows * gli_cellh) + xh;

    if (w != gli_image_w || h != gli_image_h)
	SetWindowPos(hwndframe, 0, 0, 0, w, h, SWP_NOZORDER | SWP_NOMOVE);
}

LRESULT CALLBACK
frameproc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch(message)
    {
    case WM_SETFOCUS:
	PostMessage(hwnd, WM_APP+5, 0, 0);
	return 0;
    case WM_APP+5:
	SetFocus(hwndview);
	return 0;

    case WM_DESTROY:
	PostQuitMessage(0);
	exit(1);
	break;

    case WM_SIZE:
	{
	    // More generally, we should use GetEffectiveClientRect
	    // if you have a toolbar etc.
	    RECT rect;
	    GetClientRect(hwnd, &rect);
	    MoveWindow(hwndview, rect.left, rect.top,
		    rect.right-rect.left, rect.bottom-rect.top, TRUE);
	}
	return 0;

    case WM_SIZING:
	if (0) {
	    RECT *r = (RECT*)lParam;
	    int w, h;
	    int cw, ch;
	    int xw, xh;

	    xw = gli_wmarginx * 2;
	    xh = gli_wmarginy * 2;
	    gli_calc_padding(gli_rootwin, &xw, &xh);
	    xw += GetSystemMetrics(SM_CXFRAME) * 2;
	    xh += GetSystemMetrics(SM_CYFRAME) * 2;
	    xh += GetSystemMetrics(SM_CYCAPTION);

	    w = r->right - r->left - xw;
	    h = r->bottom - r->top - xh;

	    cw = w / gli_cellw;
	    ch = h / gli_cellh;

	    if (ch < 10) ch = 10;
	    if (cw < 30) cw = 30;
	    if (ch > 200) ch = 200;
	    if (cw > 250) cw = 250;

	    w = (cw * gli_cellw) + xw;
	    h = (ch * gli_cellh) + xh;

	    if (wParam == WMSZ_TOPRIGHT ||
		    wParam == WMSZ_RIGHT ||
		    wParam == WMSZ_BOTTOMRIGHT)
		r->right = r->left + w;
	    else
		r->left = r->right - w;

	    if (wParam == WMSZ_BOTTOMLEFT ||
		    wParam == WMSZ_BOTTOM ||
		    wParam == WMSZ_BOTTOMRIGHT)
		r->bottom = r->top + h;
	    else
		r->top = r->bottom - h;

	    return 1;
	}
	break;

    case WM_SYSCOMMAND:
	if (wParam == ID_ABOUT)
	{
	    onabout();
	    return 0;
	}
	if (wParam == ID_CONFIG)
	{
	    onconfig();
	    return 0;
	}
	if (wParam == ID_TOGSCR)
	{
	    if (gli_scroll_width)
		gli_scroll_width = 0;
	    else
		gli_scroll_width = 8;
	    gli_force_redraw = 1;
	    gli_windows_size_change();
	    return 0;
	}
	break;

    case WM_TIMER:
	timeouts ++;
	return 0;

    case WM_NOTIFY:
    case WM_COMMAND:
	return SendMessage(hwndview, message, wParam, lParam);
    }
    return DefWindowProc(hwnd, message, wParam, lParam);
}

#define Uni_IsSurrogate1(ch) ((ch) >= 0xD800 && (ch) <= 0xDBFF)
#define Uni_IsSurrogate2(ch) ((ch) >= 0xDC00 && (ch) <= 0xDFFF)
 
#define Uni_SurrogateToUTF32(ch, cl) (((ch) - 0xD800) * 0x400 + ((cl) - 0xDC00) + 0x10000)
 
#define Uni_UTF32ToSurrogate1(ch) (((ch) - 0x10000) / 0x400 + 0xD800)
#define Uni_UTF32ToSurrogate2(ch) (((ch) - 0x10000) % 0x400 + 0xDC00)

LRESULT CALLBACK
viewproc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    int x = (signed short) LOWORD(lParam);
    int y = (signed short) HIWORD(lParam);
    glui32 key;

    switch (message)
    {
    case WM_ERASEBKGND:
	return 1; // don't erase; we'll repaint it all

    case WM_PAINT:
	{
	    PAINTSTRUCT ps;

	    /* make sure we have a fresh bitmap */
	    gli_windows_redraw();

	    /* and blit it to the screen */
	    hdc = BeginPaint(hwnd, &ps);
	    winblit(ps.rcPaint);
	    hdc = NULL;
	    EndPaint(hwnd, &ps);

	    return 0;
	}

    case WM_SIZE:
	{
	    int newwid = LOWORD(lParam);
	    int newhgt = HIWORD(lParam);

	    if (newwid == 0 || newhgt == 0)
		break;

	    if (newwid == gli_image_w && newhgt == gli_image_h)
		break;

	    gli_image_w = newwid;
	    gli_image_h = newhgt;

	    gli_image_s = ((gli_image_w * 3 + 3) / 4) * 4;
	    if (gli_image_rgb)
		free(gli_image_rgb);
	    gli_image_rgb = malloc(gli_image_s * gli_image_h);

	    gli_force_redraw = 1;

	    gli_windows_size_change();

	    break;
	}

    case WM_LBUTTONDOWN:
	gli_input_handle_click(x, y);
	return 0;

    case WM_KEYDOWN:

	switch (wParam)
	{
	case VK_PRIOR: gli_input_handle_key(keycode_PageUp); break;
	case VK_NEXT: gli_input_handle_key(keycode_PageDown); break;
	case VK_HOME: gli_input_handle_key(keycode_Home); break;
	case VK_END: gli_input_handle_key(keycode_End); break;
	case VK_LEFT: gli_input_handle_key(keycode_Left); break;
	case VK_RIGHT: gli_input_handle_key(keycode_Right); break;
	case VK_UP: gli_input_handle_key(keycode_Up); break;
	case VK_DOWN: gli_input_handle_key(keycode_Down); break;
	case VK_ESCAPE: gli_input_handle_key(keycode_Escape); break;
	case VK_F1: gli_input_handle_key(keycode_Func1); break;
	case VK_F2: gli_input_handle_key(keycode_Func2); break;
	case VK_F3: gli_input_handle_key(keycode_Func3); break;
	case VK_F4: gli_input_handle_key(keycode_Func4); break;
	case VK_F5: gli_input_handle_key(keycode_Func5); break;
	case VK_F6: gli_input_handle_key(keycode_Func6); break;
	case VK_F7: gli_input_handle_key(keycode_Func7); break;
	case VK_F8: gli_input_handle_key(keycode_Func8); break;
	case VK_F9: gli_input_handle_key(keycode_Func9); break;
	case VK_F10: gli_input_handle_key(keycode_Func10); break;
	case VK_F11: gli_input_handle_key(keycode_Func11); break;
	case VK_F12: gli_input_handle_key(keycode_Func12); break;
	}
	return 0;

	/* unicode encoded chars, including escape, backspace etc... */
	case WM_UNICHAR:
		key = wParam;

		if (key == UNICODE_NOCHAR)
			return 1; /* yes, we like WM_UNICHAR */

		if (key == '\r' || key == '\n')
			gli_input_handle_key(keycode_Return);
		else if (key == '\b')
			gli_input_handle_key(keycode_Delete);
		else if (key == '\t')
			gli_input_handle_key(keycode_Tab);
		else if (key != 27)
			gli_input_handle_key(key);

		return 0;

    case WM_CHAR:
		key = wParam;

		if (key == '\r' || key == '\n')
			gli_input_handle_key(keycode_Return);
		else if (key == '\b')
			gli_input_handle_key(keycode_Delete);
		else if (key == '\t')
			gli_input_handle_key(keycode_Tab);
		else if (key != 27) {
			/* translate from ANSI code page to Unicode */
			char ansich = (char)key;
			wchar_t widebuf[2];
			int res = MultiByteToWideChar(CP_ACP, 0, &ansich, 1, widebuf, 2);
			if (res) {
				if (Uni_IsSurrogate1(widebuf[0]))
					key = Uni_SurrogateToUTF32(widebuf[0], widebuf[1]);
				else
					key = widebuf[0];
				gli_input_handle_key(key);
			}
		}

		return 0;
    }

    /* Pass on unhandled events to Windows */
    return DefWindowProc(hwnd, message, wParam, lParam);
}

