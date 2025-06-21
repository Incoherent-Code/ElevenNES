using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms.Controls;

namespace ElevenNES {
   public class ElevenNES : Game {
      public GraphicsDeviceManager Graphics;
      public SpriteBatch SpriteBatch;
      public UI.UI UI;
      public static int Scale = 3;

      public ElevenNES() {
         Graphics = new GraphicsDeviceManager(this) {
            PreferredBackBufferHeight = 240 * Scale,
            PreferredBackBufferWidth = 280 * Scale
         };
         Graphics.ApplyChanges();

         Window.Title = "ElevenNES";

         Content.RootDirectory = "Content";
         IsMouseVisible = true;
      }

      protected override void Initialize() {
         InitializeGum();
         UI = new UI.UI(this);

         base.Initialize();
      }

      protected override void LoadContent() {
         SpriteBatch = new SpriteBatch(GraphicsDevice);

         // TODO: use this.Content to load your game content here
      }

      protected override void Update(GameTime gameTime) {
         if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

         GumService.Default.Update(gameTime);

         base.Update(gameTime);
      }

      protected override void Draw(GameTime gameTime) {
         GraphicsDevice.Clear(Color.Black);

         GumService.Default.Draw();

         base.Draw(gameTime);
      }
      private void InitializeGum() {
         // Initialize the Gum service
         GumService.Default.Initialize(this);
         GumService.Default.ContentLoader.XnaContentManager = Content;

         // Register keyboard input for UI control.
         FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);

         // Register gamepad input for Ui control.
         FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);

         // Customize the tab reverse UI navigation to also trigger when the keyboard
         // Up arrow key is pushed.
         FrameworkElement.TabReverseKeyCombos.Add(
            new KeyCombo() { PushedKey = Keys.Up });

         // Customize the tab UI navigation to also trigger when the keyboard
         // Down arrow key is pushed.
         FrameworkElement.TabKeyCombos.Add(
            new KeyCombo() { PushedKey = Keys.Down });

         GumService.Default.CanvasWidth = this.Window.ClientBounds.Width;
         GumService.Default.CanvasHeight = this.Window.ClientBounds.Height;
         GumService.Default.Renderer.Camera.Zoom = 1f;
      }

   }
}
