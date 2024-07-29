using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace FontExporter
{

    public class FontExporter : Mod
	{
        public class MyModUI : UIState
        {
            private UIPanel _container;
            private UICharacterNameButton _pathPlate;

            public override void OnInitialize()
            {
                var lel = Language.GetOrRegister("Mods.FontExporter.Path", () => { return "Full Path: "; });
                var nothing = Language.GetOrRegister("Mods.FontExporter.Nothing", () => { return ""; });

                var width = Main.instance.Window.ClientBounds.Width;
                var height = Main.instance.Window.ClientBounds.Height;

                _container = new UIPanel()
                {
                    Width = StyleDimension.FromPixels(720),
                    Height = StyleDimension.FromPixels(260),
                    Left = StyleDimension.FromPixels(width / 2 - 360),
                    Top = StyleDimension.FromPixels(height / 2 - 130)
                };
                
                _pathPlate = new UICharacterNameButton(lel, nothing, nothing)
                {
                    Width = StyleDimension.FromPixels(620),
                    Height = StyleDimension.FromPixels(75),
                    Left = StyleDimension.FromPixels(50),
                    Top = StyleDimension.FromPixels(20)
                };
                
                _pathPlate.OnLeftClick += Click_SetName;
                _pathPlate.SetSnapPoint("SelectPath", 0);

                // Create the "Accept" button
                var _acceptButton = new UIButton<string>("Accept")
                {
                    Width = StyleDimension.FromPixels(200),
                    Height = StyleDimension.FromPixels(50),
                    Left = StyleDimension.FromPixels(260),
                    Top = StyleDimension.FromPixels(125)
                };
                _acceptButton.OnLeftClick += AcceptButtonClicked;

                var _cancelButton = new UIButton<string>("Cancel")
                {
                    Width = StyleDimension.FromPixels(175),
                    Height = StyleDimension.FromPixels(50),
                    Left = StyleDimension.FromPixels(272.5f),
                    Top = StyleDimension.FromPixels(180)
                };
                _cancelButton.OnLeftClick += CancelButtonClicked;

                _container.Append(_pathPlate);
                _container.Append(_acceptButton);
                _container.Append(_cancelButton);

                // Add the container to the UIState
                Append(_container);

            }
            private void AcceptButtonClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                path = path ?? "";
                path = path.Replace("\\", "/");
                if (path.Length < 3 || !path.Substring(0, Math.Min(5, path.Length)).Contains("/"))
                {
                    Click_SetName(evt, listeningElement);
                    return;
                }
                CancelButtonClicked(evt, listeningElement);
                On_Main.Update += On_Main_Update;
            }
            private void CancelButtonClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                On_Main.Update -= On_Main_UpdatePre;
                MyUI = null;
                Main.MenuUI.SetState(null);
                Main.gameMenu = true;
                Main.menuMode = 0;
            }

            private void Click_SetName(UIMouseEvent evt, UIElement listeningElement)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.clrInput();
                UIVirtualKeyboard uIVirtualKeyboard = new UIVirtualKeyboard("Enter full path to the folder that contains \"Data.txt\" file with the characters you want to export:", "", OnFinishedSettingName, GoBackHere, 0, allowEmpty: false);
                uIVirtualKeyboard.SetMaxInputLength(32767);
                Main.MenuUI.SetState(uIVirtualKeyboard);
            }

            private void OnFinishedSettingName(string name)
            {
                path = name;
                if (!path.EndsWith("\\"))
                {
                    path += "\\";
                }
                UpdateInputFields();
                GoBackHere();
            }

            private void UpdateInputFields()
            {
                _pathPlate.SetContents(path);
                _pathPlate.Recalculate();
            }

            private void GoBackHere()
            {
                Main.MenuUI.SetState(this);
            }
        }
        public override void PostAddRecipes()
        {
            On_Main.Update += On_Main_UpdatePre;
            typeof(ModLoader).GetField("OnSuccessfulLoad", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
        }
        public static MyModUI MyUI = new MyModUI();

        public static void On_Main_UpdatePre(On_Main.orig_Update orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);
            if (MyUI == null) return;
            if (Main.MenuUI.CurrentState != null && (Main.MenuUI.CurrentState.GetType() == typeof(MyModUI) ||
                Main.MenuUI.CurrentState.GetType() == typeof(UIVirtualKeyboard))) return;

            typeof(Main).GetField("_blockFancyUIWhileLoading", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, false);
            Main.menuMode = 888;
            Main.MenuUI.SetState(MyUI);
        }

        public static string path;
        public static void On_Main_Update(On_Main.orig_Update orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);
            On_Main.Update -= On_Main_Update;

            try
            {
                var font = FontAssets.MouseText.Value;

                var method = typeof(DynamicSpriteFont).GetMethod("GetCharacterData", BindingFlags.NonPublic | BindingFlags.Instance);
                //var spriteCharacterData = typeof(DynamicSpriteFont).GetNestedType("SpriteCharacterData", BindingFlags.NonPublic);

                string Data = File.ReadAllText(path + "Data.txt", Encoding.UTF8).Replace("\t", "").Replace("\r", "").Replace("\n", "");

                List<(char, int)> charPoses = new();

                string ToGetData = "";
                for (int i = 0; i < Data.Length; i++)
                {
                    ToGetData += " " + Data[i];
                }
                ToGetData += " ";

                for (int i = 1; i < ToGetData.Length; i+=2)
                {
                    charPoses.Add((ToGetData[i], (int)font.MeasureString(ToGetData.Substring(0, i + 1)).X));
                }
                var size = font.MeasureString(ToGetData);

                RenderTarget2D renderTarget = new(
                    self.GraphicsDevice,
                    (int)size.X, (int)size.Y,
                    false,
                    SurfaceFormat.Alpha8,
                    DepthFormat.None,
                    0,
                    RenderTargetUsage.DiscardContents
                );


                self.GraphicsDevice.SetRenderTarget(renderTarget);

                self.GraphicsDevice.Clear(Color.Transparent);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null);

                Utils.DrawBorderString(Main.spriteBatch, ToGetData, Vector2.Zero, Color.White);

                Main.spriteBatch.End();

                self.GraphicsDevice.SetRenderTarget(null);

                using (FileStream stream = File.OpenWrite(path + "chars.png"))
                {
                    renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
                }
                using (FileStream stream = File.OpenWrite(path + "poses.txt"))
                {
                    string str = "";
                    foreach (var item in charPoses)
                    {
                        str += "[" + item.Item1 + "] - " + item.Item2 + "\n";
                    }
                    stream.Write(str.ToByteArray());
                }
                MessageBox.Show("Export Completed!", "Success");
                Main.MenuUI.SetState(null); 
            }
            catch (Exception e)
            {
                var res = MessageBox.Show(e.Message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (res == DialogResult.OK) MessageBox.Show(e.StackTrace, "Error");
                Main.MenuUI.SetState(null);
            }
        }
    }
}
