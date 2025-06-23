using System;
using System.Runtime.CompilerServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.Managers;
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
            Height = 240 * ElevenNES.Scale,
            Width = 280 * ElevenNES.Scale,
         };
         //RootPanel.Anchor(Gum.Wireframe.Anchor.Center);
         //InteralPanel.AddChild(RootPanel);
         RootPanel.AddToRoot();
         TopBar = new ContainerRuntime() {
            Height = 16 * ElevenNES.Scale,
         };
         TopBar.Dock(Gum.Wireframe.Dock.Top);
         RootPanel.AddChild(TopBar);

         var backgroundTile = new ColoredRectangleRuntime() {
            Color = Color.LightGray
         };
         backgroundTile.Dock(Gum.Wireframe.Dock.Fill);
         TopBar.AddChild(backgroundTile);

         FileButton.Visual.X = 1 * ElevenNES.Scale;
         FileButton.Visual.Y = 1 * ElevenNES.Scale;
         TopBar.AddChild(FileButton);
         FileButton.Click += (_, _) => {
            var result = Dialog.FileOpen("nes");
            if (result.IsOk) {
               Game.ChangeGame(result.Path);
            }
         };

      }

   }
}
