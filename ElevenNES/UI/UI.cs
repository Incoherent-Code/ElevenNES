using System;
using System.Runtime.CompilerServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.Managers;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using NativeFileDialogSharp;

namespace ElevenNES.UI {
   public class UI {
      /// <summary>
      /// RootPanel will stay in the center of the window, while this window allows the desktop window to be resized without issue.
      /// </summary>
      private ContainerRuntime InteralPanel;
      private void UpdateInternalPanel() {
         InteralPanel.Width = Game.Window.ClientBounds.Width;
         InteralPanel.Height = Game.Window.ClientBounds.Height;
      }
      /// <summary>
      /// Main Panel where all ui elements should inhabit.
      /// </summary>
      public ContainerRuntime RootPanel;
      public ContainerRuntime TopBar;
      public TopButton FileButton = new TopButton("File");
      public Dropdown FileDropdown;
      private ElevenNES Game;
      public UI(ElevenNES game) {
         Game = game;

         //Needs more debugging
         //Intended to support window resizing.
         //InteralPanel = new ContainerRuntime() {
         //   X = 0,
         //   Y = 0,
         //};
         //UpdateInternalPanel();
         //Game.Window.ClientSizeChanged += (_, _) => UpdateInternalPanel();
         //InteralPanel.AddToRoot();

         RootPanel = new ContainerRuntime() {
            Height = 240 * Config.Instance.Scale,
            Width = 280 * Config.Instance.Scale,
         };
         //RootPanel.Anchor(Gum.Wireframe.Anchor.Center);
         //InteralPanel.AddChild(RootPanel);
         RootPanel.AddToRoot();
         TopBar = new ContainerRuntime() {
            Height = 16 * Config.Instance.Scale,
         };
         TopBar.Dock(Gum.Wireframe.Dock.Top);
         RootPanel.AddChild(TopBar);

         var backgroundTile = new ColoredRectangleRuntime() {
            Color = new Color(0, 0, 127)
         };
         backgroundTile.Dock(Gum.Wireframe.Dock.Fill);
         TopBar.AddChild(backgroundTile);

         FileDropdown = new Dropdown([
            new DropdownEntryInfo("Open File...", Keys.O, () => {
               var result = Dialog.FileOpen("nes");
               if (result.IsOk) {
                  Game.ChangeGame(result.Path);
               }
            }), 
            new DropdownEntryInfo("Reset", Keys.R, () => {
               var current = Game.NESEmulator.CurrentGamePath;
               Game.ChangeGame(current);
            })
         ]);
         FileDropdown.Y = 16 * Config.Instance.Scale;
         FileDropdown.X = 0;
         RootPanel.AddChild(FileDropdown);

         FileButton.Visual.X = 2 * Config.Instance.Scale;
         FileButton.Visual.Y = 2 * Config.Instance.Scale;
         TopBar.AddChild(FileButton);
         FileButton.Click += (_, _) => {
            FileDropdown.Visible = !FileDropdown.Visible;
         };

      }

   }
}
