
using System;
using ImGuiNET;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CSharpManager;

public class GameConsole
{
    #region 控制台渲染
    /// <summary>
    /// 控制台输入区域高度
    /// </summary>
    private const float _consoleInputAreaHeight = 40;
    /// <summary>
    /// 控制台输出区域高度
    /// </summary>
    private const float _consoleOutputAreaHeight = 500;
    /// <summary>
    /// 是否显示控制台
    /// </summary>
    private bool _showConsole = false;
    /// <summary>
    /// 是否显示控制台输出区域
    /// </summary>
    private bool _showConsoleOutputArea = false;
    /// <summary>
    /// 是否聚焦在控制台输入框
    /// </summary>
    private bool _focusInputText = false;
    /// <summary>
    /// 输入历史记录
    /// </summary>
    private List<string> _inputHistory = new List<string>();
    /// <summary>
    /// 历史记录索引
    /// </summary>
    private int _historyIndex = -1;
    /// <summary>
    /// 当前输入文本
    /// </summary>
    private string _inputText = "";

    /// <summary>
    /// 开关控制台显示
    /// </summary>
    public void ToggleConsole()
    {
        _showConsole = !_showConsole;
    }
    /// <summary>
    /// 帧渲染函数
    /// </summary>
    public void Render()
    {
        // 如果不显示控制台则直接返回
        if (!_showConsole)
        {
            return;
        }
        // 获得屏幕宽高
        var io = ImGui.GetIO();
        var screenWidth = io.DisplaySize.X;
        var screenHeight = io.DisplaySize.Y;
        // 如果按下F2键则切换控制台详细信息窗口的显示和隐藏
        if (ImGui.IsKeyPressed(ImGuiKey.F2))
        {
            _showConsoleOutputArea = !_showConsoleOutputArea;
        }
        // 渲染控制台详细信息窗口
        if (_showConsoleOutputArea)
        {
            // 设置详细信息窗口的位置和大小
            ImGui.SetNextWindowPos(new Vector2(0, screenHeight - _consoleInputAreaHeight - _consoleOutputAreaHeight));
            ImGui.SetNextWindowSize(new Vector2(screenWidth, _consoleOutputAreaHeight));
            ImGuiWindowFlags outputWindowFlags = ImGuiWindowFlags.NoTitleBar |
                                                 ImGuiWindowFlags.NoResize |
                                                 ImGuiWindowFlags.NoMove |
                                                 ImGuiWindowFlags.NoSavedSettings |
                                                 ImGuiWindowFlags.NoCollapse;

            ImGui.Begin("ConsoleOutputArea", outputWindowFlags);
            // 设置滚动区域，作为控制台历史记录的显示区域
            ImGui.BeginChild("ScrollRegion", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            // 遍历历史记录并显示
            foreach (var entry in _inputHistory)
            {
                ImGui.Text(entry);
            }
            ImGui.EndChild();
            ImGui.End();
        }
        // 渲染控制台输入区域，设置位置和大小
        ImGui.SetNextWindowPos(new Vector2(0, screenHeight - _consoleInputAreaHeight));
        ImGui.SetNextWindowSize(new Vector2(screenWidth, _consoleInputAreaHeight));
        ImGui.SetNextWindowBgAlpha(1f);
        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar |
                                       ImGuiWindowFlags.NoResize |
                                       ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoScrollbar |
                                       ImGuiWindowFlags.NoSavedSettings;

        ImGui.Begin("ConsoleInputArea", windowFlags);
        float windowWidth = ImGui.GetWindowWidth();
        ImGui.SetNextItemWidth(windowWidth);
        Vector4 windowBgColor = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
        ImGui.PushStyleColor(ImGuiCol.FrameBg, windowBgColor);
        ImGuiInputTextFlags inputTextFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackHistory;
        // 输入框渲染
        unsafe
        {
            // 聚焦输入框，实现每次输入完成后自动聚焦
            if (_focusInputText)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputText = false;
            }
            // 渲染输入框并监听输入回调
            if (ImGui.InputText("##ConsoleInput", ref _inputText, maxLength: 256, inputTextFlags, TextEditCallback))
            {
                if (!string.IsNullOrEmpty(_inputText))
                {
                    // 分割输入文本，以空格为分隔符
                    string[] input = _inputText.Split(' ');
                    // 第一个参数为命令，后续参数为参数
                    string command = input[0];
                    string[] args = input.Length > 1 ? input.Skip(1).ToArray() : Array.Empty<string>();
                    // 如果命令存在则执行
                    if (_commands.ContainsKey(command))
                    {
                        _commands[command]?.Invoke(args);
                    }
                    _inputHistory.Add(_inputText); // 添加历史记录
                    _inputText = ""; // 清空输入文本
                    _historyIndex = -1; // 重置历史记录索引
                }
                // 自动聚焦输入框
                _focusInputText = true;
            }
        }
        ImGui.PopStyleColor();
        ImGui.End();
    }

    /// <summary>
    /// 监听输入文本回调
    /// </summary>
    /// <param name="data">输入文本数据</param>
    /// <returns>状态</returns>
    private unsafe int TextEditCallback(ImGuiInputTextCallbackData* data)
    {
        // 只有当输入文本回调是历史记录时才处理
        if (data->EventFlag != ImGuiInputTextFlags.CallbackHistory)
        {
            return 0;
        }
        // 监听上下方向键，读取历史记录并显示
        if (data->EventKey == ImGuiKey.UpArrow)
        {
            if (_inputHistory.Count > 0)
            {
                if (_historyIndex == -1)
                {
                    _historyIndex = _inputHistory.Count - 1;
                }
                else if (_historyIndex > 0)
                {
                    _historyIndex--;
                }
                SetBufferText(data, _inputHistory[_historyIndex]);
            }
        }
        else if (data->EventKey == ImGuiKey.DownArrow)
        {
            if (_inputHistory.Count > 0 && _historyIndex >= 0)
            {
                if (_historyIndex < _inputHistory.Count - 1)
                {
                    _historyIndex++;
                }
                else
                {
                    _historyIndex = -1;
                }
                SetBufferText(data, _historyIndex >= 0 ? _inputHistory[_historyIndex] : "");
            }
        }
        return 0;
    }
    /// <summary>
    /// 设置文本缓冲区
    /// </summary>
    /// <param name="data">文本数据</param>
    /// <param name="text">要设置的字符串</param>
    private unsafe void SetBufferText(ImGuiInputTextCallbackData* data, string text)
    {
        byte* ptr = data->Buf;
        int maxSize = data->BufSize - 1;
        int len = Math.Min(text.Length, maxSize);

        for (int i = 0; i < len; i++)
        {
            ptr[i] = (byte)text[i];
        }
        ptr[len] = 0;

        data->BufDirty = 1;
        data->BufTextLen = len;
        data->CursorPos = len;
        data->SelectionStart = len;
        data->SelectionEnd = len;
    }

    #endregion 控制台渲染

    #region 控制台命令

    private Dictionary<string, Action<string[]>> _commands = new();
    public void RegisterCommand(string command, Action<string[]> action)
    {
        _commands[command] = action;
    }
    public void Log(string message)
    {
        _inputHistory.Add(message);
    }

    #endregion 控制台命令
}