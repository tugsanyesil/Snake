using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Snake
{
    public partial class Form : System.Windows.Forms.Form
    {
        public enum Page
        {
            Main = 0,
            Info = 1,
            End = 2,
            Game = 3,
        }

        public enum Speed
        {
            Easy = 1,
            Normal = 2,
            Hard = 4,
        }

        public Form()
        {
            InitializeComponent();
        }

        readonly string[] PageTexts =
            {
                "  I to Info  \n\nSpace to Game",
                "W, A, S, D to Control\n   Up to Speed Up   \n Down to Speed Down \nSpace to Refresh Game\n     Esc to Main     ",
                "     End     \n\n Esc to Main \nSpace to Game"
            };
        readonly Brush[] PageBrushes = { Brushes.White, Brushes.Wheat, Brushes.Red };
        readonly int[] PageFontSizes = { 25, 15, 20 };

        readonly Color EmptyColor = Color.Black;
        readonly Color SnakeTailColor = Color.Green;
        readonly Color SnakeHeadColor = Color.DarkGreen;
        readonly Color FoodColor = Color.Purple;
        readonly Size GameSize = new Size(30, 30);
        readonly Size Zoom = new Size(10, 10);

        public Action GameSpeedChanged;
        public Speed GameSpeed
        {
            get => gameSpeed;
            set
            {
                gameSpeed = value;
                GameSpeedChanged?.Invoke();
            }
        }
        Speed gameSpeed;
        readonly int EasySpeedInterval = 150;

        private void GameSpeed_Changed()
        {
            Timer.Interval = EasySpeedInterval / (int)GameSpeed;
        }

        List<Point> Snake = new List<Point>() { Point.Empty };
        Point SnakeHead => Snake[0];
        Point Food;
        Point Direction;
        bool InGame;

        Size ScreenSize => SizeScale(GameSize, Zoom);
        Bitmap EmptyScreenBitmap => new Bitmap(ScreenSize.Width, ScreenSize.Height);
        Bitmap ScreenBitmap;

        public Action AppPageChanged;
        public Page AppPage
        {
            get => appPage;
            set
            {
                appPage = value;
                AppPageChanged?.Invoke();
            }
        }
        private Page appPage;

        private void AppPage_Changed()
        {
            switch (appPage)
            {
                case Page.Main:
                case Page.Info:
                case Page.End:
                    {
                        Direction = Point.Empty;
                        InGame = false;

                        Timer.Enabled = false;

                        int i = (int)appPage;

                        using (Graphics g = Graphics.FromImage(ScreenBitmap))
                        {
                            ClearScreen(g);
                            WriteScreen(g, PageFontSizes[i], PageBrushes[i], PageTexts[i]);
                        }
                        Game.Image = ScreenBitmap;

                        break;
                    }
                case Page.Game:
                    {
                        Direction = Point.Empty;

                        Snake = new List<Point>() { RandomPoint() };
                        SnakeGrowUp();

                        Game.Image = EmptyScreenBitmap;
                        GameBitmapRefresh();

                        Timer.Enabled = true;

                        break;
                    }
            }
        }

        private Random Random = new Random();
        private void Form_Load(object sender, EventArgs e)
        {
            ScreenBitmap = new Bitmap(ScreenSize.Width, ScreenSize.Height);

            GameSpeedChanged += GameSpeed_Changed;
            GameSpeed = Speed.Normal;

            AppPageChanged += AppPage_Changed;
            AppPage = Page.Main;

            WriteInfo();
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            switch (appPage)
            {
                case Page.Main:
                    {
                        switch (e.KeyCode)
                        {
                            case Keys.I: { AppPage = Page.Info; break; }
                            case Keys.Space: { AppPage = Page.Game; break; }
                        }
                        break;
                    }
                case Page.Info:
                    {
                        switch (e.KeyCode)
                        {
                            case Keys.Escape: { AppPage = Page.Main; break; }
                            case Keys.Up: { GameSpeed = GameSpeed == Speed.Easy ? Speed.Normal : Speed.Hard; WriteInfo(); break; } // SpeedUp
                            case Keys.Down: { GameSpeed = GameSpeed == Speed.Hard ? Speed.Normal : Speed.Easy; WriteInfo(); break; } // SpeedDown
                            case Keys.Space: { AppPage = Page.Game; break; }
                        }
                        break;
                    }
                case Page.End:
                    {
                        switch (e.KeyCode)
                        {
                            case Keys.Escape: { AppPage = Page.Main; break; }
                            case Keys.Space: { AppPage = Page.Game; break; }
                        }
                        break;
                    }
                case Page.Game:
                    {
                        Direction = Point.Empty;
                        switch (e.KeyCode)
                        {
                            case Keys.W: { Direction.Y -= 1; break; }
                            case Keys.A: { Direction.X -= 1; break; }
                            case Keys.S: { Direction.Y += 1; break; }
                            case Keys.D: { Direction.X += 1; break; }
                            case Keys.Space: { if (!InGame) { AppPage = Page.Game; } break; }
                        }
                        break;
                    }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Direction != Point.Empty)
            {
                if (!InGame) { InGame = true; }

                var newhead = PointSum(SnakeHead, Direction);
                if (IsInGame(newhead) && !IsOnSnake(newhead))
                {
                    Snake.Insert(0, newhead);
                    if (SnakeHead == Food) { SnakeGrowUp(); }
                    else { SnakeMove(); }

                    GameBitmapRefresh();
                }
                else
                {
                    AppPage = Page.End;
                }
            }
        }

        void GameBitmapRefresh()
        {
            ScreenBitmap = new Bitmap(ScreenSize.Width, ScreenSize.Height);
            Bitmap Unit = new Bitmap(Zoom.Width, Zoom.Height);

            using (var g = Graphics.FromImage(ScreenBitmap))
            using (var gu = Graphics.FromImage(Unit))
            {
                ClearScreen(g);

                PaintScreen(g, gu, Unit, SnakeTailColor, Snake);
                PaintScreen(g, gu, Unit, SnakeHeadColor, new List<Point> { SnakeHead });
                PaintScreen(g, gu, Unit, FoodColor, new List<Point> { Food });
            }

            Game.Image = ScreenBitmap;
        }

        PointF RandomPointF() => new PointF((float)Random.NextDouble(), (float)Random.NextDouble());
        Point RandomPoint()
        {
            var p = PointScale(RandomPointF(), GameSize);
            return new Point((int)p.X, (int)p.Y);
        }

        bool IsInGame(Point p) => 0 <= p.X && p.X < GameSize.Width && 0 <= p.Y && p.Y < GameSize.Height;

        bool IsOnSnake(Point p) => !Snake.TrueForAll(ps => ps != p);

        void PutFood() { do { Food = RandomPoint(); } while (IsOnSnake(Food)); }

        PointF MiddlePoint(SizeF size) => new PointF((ScreenSize.Width - size.Width) / 2, (ScreenSize.Height - size.Height) / 2);

        void ClearScreen(Graphics g) => g.Clear(EmptyColor);

        void WriteScreen(Graphics g, int fontsize, Brush brush, string text)
        {
            var font = new Font(FontFamily.GenericMonospace, fontsize);
            g.DrawString(text, font, brush, MiddlePoint(g.MeasureString(text, font)));
        }

        Point PointScale(Point p, Size s) => new Point(p.X * s.Width, p.Y * s.Height);

        PointF PointScale(PointF p, Size s) => new PointF(p.X * s.Width, p.Y * s.Height);

        Size SizeScale(Size s1, Size s2) => new Size(s1.Width * s2.Width, s1.Height * s2.Height);

        Point PointSum(Point p1, Point p2) => new Point(p1.X + p2.X, p1.Y + p2.Y);

        void PaintScreen(Graphics g, Graphics gu, Bitmap Unit, Color color, List<Point> Points)
        {
            gu.Clear(color);
            Points.ForEach(p => { g.DrawImage(Unit, PointScale(p, Zoom)); });
        }

        void WriteInfo()
        {
            InfoLabel.Text = $"Score : {Snake.Count - 1}                        Speed : {GameSpeed}";
        }

        void SnakeGrowUp()
        {
            WriteInfo();
            PutFood();
        }
        void SnakeMove()
        {
            Snake.RemoveAt(Snake.Count - 1);
        }
    }
}