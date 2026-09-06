using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Dral.Sprites;

using var game = new TestGame();
game.Run();

public class TestGame : Game
{
  private GraphicsDeviceManager _graphics;
  private SpriteBatch? _spriteBatch;
  private Texture2D? _texture;

  public TestGame()
  {
    _graphics = new GraphicsDeviceManager(this);
  }

  protected override void LoadContent()
  {
    _spriteBatch = new SpriteBatch(GraphicsDevice);

    // Using FileStream avoids file-locking issues
    using var stream = File.OpenRead("texture.png");
    _texture = Texture2D.FromStream(GraphicsDevice, stream);
  }

  protected override void Draw(GameTime gameTime)
  {
    var blackTexture = new Texture2D(this._graphics.GraphicsDevice, 1, 1);
    blackTexture.SetData([Color.Black]);

    var redTexture = new Texture2D(this._graphics.GraphicsDevice, 1, 1);
    redTexture.SetData([Color.Red]);


    GraphicsDevice.Clear(Color.CornflowerBlue);

    if (_spriteBatch != null && _texture != null)
    {
      _spriteBatch.Begin();

      var x = _texture.Clip(new Rectangle(0, 32, 16, 32)).ProjectTo(new(0, 0, 32, 64));

      var y = new Sprite([
        blackTexture.Clip(blackTexture.Bounds).ProjectTo(new (64, 64, 64, 64)),
        redTexture.Clip(redTexture.Bounds).ProjectTo(new (64, 128, 64, 64)),
        x.Scale(new(2, 2), new(0, 0)).Translate(new(64, 64)),
      ]).Scale(new(2, 2), new(0, 0));

      // _spriteBatch.Draw(redTexture, new(64, 128, 64, 64), blackTexture.Bounds, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
      // _spriteBatch.Draw(blackTexture, new(64, 64, 64, 64), blackTexture.Bounds, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
      // x.Draw(_spriteBatch, new(64, 64), Color.White, 0, Vector2.Zero, new Vector2(2, 2), SpriteEffects.None, 0);
      y.Draw(_spriteBatch, new(0, 0), Color.White, 0, Vector2.Zero, new Vector2(1, 1), SpriteEffects.None, 0);

      // _spriteBatch.Draw(x.Clip(new Rectangle(0, 0, x.Width, x.Height)), new Vector2(100, 100), Color.White);

      _spriteBatch.End();
    }

    base.Draw(gameTime);
  }
}
