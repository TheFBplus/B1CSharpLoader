using ClickableTransparentOverlay;
using System.Threading.Tasks;
using ImGuiNET;
using System.IO;
using CSharpModBase;
using CSharpModBase.Input;
using System;

namespace CSharpManager
{

    internal class ImGuiOverlay : Overlay
    {
        private bool wantKeepDemoWindow = false;
        private bool isDrawingUI = true;
        private bool isDrawingModsUI = true;
        private bool showMouse = true;
        public bool IsDrawingUI { get => isDrawingUI; }
        public bool IsDrawingModsUI { get => isDrawingUI && isDrawingModsUI; }

        public ImGuiOverlay() : base()
        {
        }

        protected override Task PostInitialized()
        {
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard |
                ImGuiConfigFlags.NavEnableGamepad |
                ImGuiConfigFlags.DockingEnable;
            io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

            const string fontPath = "c:\\Windows\\Fonts\\msyh.ttc";
            if (File.Exists(fontPath))
            {
                ReplaceFont(fontPath, (int)(18.0f * DpiScale), FontGlyphRangeType.ChineseFull);
            }
            return base.PostInitialized();
        }

        protected override void Render()
        {
            if (ClickableTransparentOverlay.Win32.Utils.IsKeyPressedAndNotTimeout(ClickableTransparentOverlay.Win32.VK.INSERT))
            {
                ToggleVisible();
            }
            var io = ImGui.GetIO();
            IntPtr foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero ||
                foregroundWindow == Window.Handle ||
                foregroundWindow == InputManager.Instance.HWnd)
            {
                if (isDrawingUI)
                {
                    ImGui.Begin("CSharpLoader by chenstack", ref isDrawingUI);
                    ImGui.Text("Press Insert to toggle window");
                    ImGui.Checkbox("Show Mouse", ref showMouse);
                    ImGui.SameLine();
                    ImGui.Checkbox("Demo Window", ref wantKeepDemoWindow);
                    // float framerate = ImGui.GetIO().Framerate;
                    // ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");
                    if (wantKeepDemoWindow)
                    {
                        ImGui.ShowDemoWindow(ref wantKeepDemoWindow);
                    }
                    ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                    isDrawingModsUI = ImGui.TreeNode("Mods UI");
                }
                var mods = CSharpModManager.Instance.LoadedMods;
                lock (mods)
                {
                    foreach (var mod in mods)
                    {
                        if (mod is IGuiMod guiMod)
                        {
                            Utils.TryRun(guiMod.Render);
                        }
                    }
                }
                if (isDrawingUI)
                {
                    if (isDrawingModsUI) ImGui.TreePop();
                    ImGui.End();
                }
                io.MouseDrawCursor = isDrawingUI && showMouse && !io.WantCaptureMouse;
            }
            else
            {
                io.MouseDrawCursor = false;
            }
        }

        public void ToggleVisible()
        {
            isDrawingUI = !isDrawingUI;
        }
    }
}
