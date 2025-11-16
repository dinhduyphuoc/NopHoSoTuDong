#Requires AutoHotkey v2.0
#SingleInstance Force

; 🔐 Tự động chạy với quyền admin nếu chưa có
if !A_IsAdmin {
    Run '*RunAs "' A_ScriptFullPath '"'
    ExitApp
}

SetTitleMatchMode(2)
CoordMode("Mouse", "Screen")

childWin := "[BỘ QUỐC PHÒNG]"   ; Cửa sổ con chọn người ký

; ====================================================
; 🔍 Dò tìm cửa sổ VGCA bằng exe + tiêu đề
; ====================================================
ToolTip("⏳ Đang dò tìm cửa sổ VGCA...")
mainHwnd := ""
Loop {
    for hwnd in WinGetList() {
        title := WinGetTitle("ahk_id " hwnd)
        proc := WinGetProcessName("ahk_id " hwnd)
        if (proc = "VGCASignService.exe" && InStr(title, "[BỘ QUỐC PHÒNG] - Ký số công văn")) {
            mainHwnd := hwnd
            break
        }
    }
    if (mainHwnd) {
        ToolTip("✅ Đã phát hiện cửa sổ VGCA.")
        Sleep(300)
        ToolTip()
        break
    }
    Sleep(100)
}
if !mainHwnd {
    ToolTip()
    MsgBox("❌ Không tìm thấy cửa sổ VGCA sau 5 giây.")
    ExitApp()
}

; ====================================================
; 🔄 Kích hoạt & đưa cửa sổ VGCA lên foreground
; ====================================================
WinActivate("ahk_id " mainHwnd)
Sleep(100)

; ====================================================
; 🖱️ Bắt đầu các thao tác ký số
; ====================================================

; --- 1️⃣ Click "Chọn vị trí ký"
ClickButtonByText(mainHwnd, "Chọn vị trí ký")

; --- 2️⃣ Click "Ký số" 
WaitAndClick(654, 112, "ahk_id " mainHwnd, "Ký số")

; --- 3️⃣ Click vào combobox chọn chữ ký
MouseMove(889, 624)
Click()

; --- 4️⃣ Gửi phím mũi tên xuống để chọn chữ ký
Send("{Down}")

; ====================================================
; ⏳ Đợi cửa sổ con [BỘ QUỐC PHÒNG] đóng
; ====================================================
ToolTip("⏳ Đang chờ cửa sổ con [BỘ QUỐC PHÒNG] đóng...")

childHwnd := 0
for hwnd in WinGetList() {
    title := WinGetTitle("ahk_id " hwnd)
    if (title = childWin) {
        childHwnd := hwnd
        break
    }
}
if (childHwnd) {
    WinWaitClose("ahk_id " childHwnd)
    ToolTip("✅ Cửa sổ chọn chữ ký đã đóng – tiến hành HOÀN THÀNH")
    Sleep(50)
    ToolTip()
} else {
    ToolTip("⚠️ Không tìm thấy cửa sổ con [BỘ QUỐC PHÒNG]")
    Sleep(50)
    ToolTip()
}

; ====================================================
; 🔍 Kiểm tra nút "HOÀN THÀNH" trước khi click
; ====================================================
foundFinish := false
for ctrl in WinGetControls("ahk_id " mainHwnd) {
    if (ControlGetText(ctrl, "ahk_id " mainHwnd) = "HOÀN THÀNH") {
        foundFinish := true
        break
    }
}

if (foundFinish) {
    ; --- 5️⃣ Click "Hoàn thành" bằng ClassNN nếu có
    ClickButtonByText(mainHwnd, "HOÀN THÀNH")
} else {
    ToolTip("❌ Không tìm thấy nút HOÀN THÀNH – tự động đóng cửa sổ VGCA.")
    Sleep(500)
    ToolTip()
    try WinClose("ahk_id " mainHwnd)
    ExitApp()
}

; ====================================================
; 🔧 Hàm chờ và click tại tọa độ trong cửa sổ
; ====================================================
WaitAndClick(x, y, winTitle, label) {
    ToolTip("⏳ Đang chờ phần tử: " label)
    Loop 30 {
        if WinActive(winTitle) {
            MouseMove(x, y)
            Sleep(100)
            Click()
            ToolTip("✔️ Đã click: " label)
            Sleep(200)
            ToolTip()
            return
        }
        Sleep(50)
    }
    ToolTip()
    MsgBox("❌ Không tìm thấy phần tử: " label)
}

ClickButtonByText(winID, buttonText) {
    for ctrl in WinGetControls("ahk_id " winID) {
        if (ControlGetText(ctrl, "ahk_id " winID) = buttonText) {
            ControlClick(ctrl, "ahk_id " winID)
            ToolTip("✅ Đã click nút: " buttonText)
            Sleep(300)
            ToolTip()
            return true
        }
    }
    MsgBox("❌ Không tìm thấy nút: " buttonText)
    return false
}
