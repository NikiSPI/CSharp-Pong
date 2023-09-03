using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Timers;
using Timer = System.Timers.Timer;

public class Frame : Form
{
    private StackFrame stackFrame = new StackTrace(new StackFrame(true)).GetFrame(0);
    public ActionPanel panel = new();
    
    public Frame()
    {
        Icon = Icon.ExtractAssociatedIcon(Path.GetDirectoryName(stackFrame.GetFileName()) + "\\Images\\PongIcon.ico");
        StartPosition = FormStartPosition.Manual;
        Location = new Point(200, 100);
        ClientSize = new Size(BackEnd.frWidth, BackEnd.frHeight);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        KeyPreview = true;
        
        Controls.Add(panel);
    }
    
    protected override void OnKeyDown(KeyEventArgs e) => MarkSelectedKeys(e, true);
    protected override void OnKeyUp(KeyEventArgs e) => MarkSelectedKeys(e, false);
    private void MarkSelectedKeys(KeyEventArgs e, bool b)
    {
        if(e.KeyCode == Keys.W)
            BackEnd.w = b;
        if(e.KeyCode == Keys.S)
            BackEnd.s = b;
        if(e.KeyCode == Keys.Up)
            BackEnd.up = b;
        if(e.KeyCode == Keys.Down)
            BackEnd.down = b;
    }

    public class ActionPanel : Panel
    {
        public ActionPanel()
        {
            Size = new Size(BackEnd.frWidth, BackEnd.frHeight);
            BackColor = Color.SkyBlue;
            DoubleBuffered = true;
        }
        
        protected override void OnPaint (PaintEventArgs e)
        {
            base.OnPaint(e);
            SolidBrush brush;
        
            e.Graphics.FillEllipse(brush = new (Color.Black), BackEnd.ballPos.X, BackEnd.ballPos.Y, BackEnd.ballSize.Width, BackEnd.ballSize.Height);
            e.Graphics.FillRectangle(brush = new(Color.Blue), BackEnd.bluePos.X, BackEnd.bluePos.Y, BackEnd.blueSize.Width, BackEnd.blueSize.Height);
            e.Graphics.FillRectangle(brush = new(Color.Red), BackEnd.redPos.X, BackEnd.redPos.Y, BackEnd.redSize.Width, BackEnd.redSize.Height);
        }
    }
    
}

class BackEnd
{
    public static Size ballSize = new Size(15, 15);
    public static PointF ballPos;

    public static Size blueSize = new(15, frHeight / 5);
    public static Point bluePos;
    public static Size redSize = new(15, frHeight / 5);
    public static Point redPos;
    public static bool w, s, up, down;

    private Frame frame = new();
    public const int frWidth = 900, frHeight = 600;

    private const float distPerFrame = 10; //total velocity
    private PointF ballMoveDist; //velocity in each axis (assigned mathematically)
    private int ballXAxis = 1;
    private Random random = new ();

    private const int frameTime = 5;
    private Timer timer = new(frameTime), delayTimer = new (1000);

    private int newRoundXAxis = 1;
    private bool ballOutBlue = false, ballOutRed = false;
    

    public void Run()
    {
        timer.Elapsed += TimerListener;
        delayTimer.Elapsed += DelayTimerListener;
        
        SetDefaultParameters();
        delayTimer.Start();
        Application.Run(frame);
    }

    private void CreateNewRound()
    {
        timer.Stop();

        newRoundXAxis *= -1;
        SetDefaultParameters();
        frame.panel.Refresh();

        Thread.Sleep(1000);
        
        timer.Start();
    }

    private void SetDefaultParameters()
    {
        ballXAxis = newRoundXAxis;
        ballMoveDist = RandomCourseAssign(1);
        ballPos = new((frWidth - ballSize.Width) / 2, (frHeight - ballSize.Height) / 2);
        
        bluePos = new(0, (frHeight - blueSize.Height) / 2);
        redPos = new(frWidth - redSize.Width, (frHeight - redSize.Height) / 2);
    }

    private PointF RandomCourseAssign(float slowDown)
    {
        ballMoveDist.Y = random.Next( (int) -(distPerFrame / 2) , (int) (distPerFrame / 2 ) ) * slowDown;
        return new PointF(ballXAxis * ( (float) Math.Sqrt(Math.Pow(distPerFrame * slowDown , 2) - Math.Pow(ballMoveDist.Y, 2)) ), ballMoveDist.Y);
    }
    private PointF LogicalCourseAssign(int tailPos, float directionChangeHardness)
    {
        ballXAxis *= -1;
        
        int halfTailSize = (blueSize.Height + ballSize.Height) / 2;
        float ballPosInTail = ballPos.Y + ballSize.Height - tailPos - halfTailSize;
        ballPosInTail /= halfTailSize;
        
        ballMoveDist.Y = ballPosInTail * distPerFrame * directionChangeHardness;
        return new PointF(ballXAxis * ( (float) Math.Sqrt(Math.Pow(distPerFrame, 2) - Math.Pow(ballMoveDist.Y, 2)) ), ballMoveDist.Y);
    }

    private void MoveTails()
    {
        if (w)
        {
            if (bluePos.Y - frHeight / 50 > 0) bluePos.Y -= frHeight / 50;
            else bluePos.Y = 0;
        }
        if (s)
        {
            if (bluePos.Y + blueSize.Height + frHeight / 50 < frHeight) bluePos.Y += frHeight / 50;
            else bluePos.Y = frHeight - blueSize.Height;
        }
        if (up)
        {
            if (redPos.Y - frHeight / 50 > 0) redPos.Y -= frHeight / 50;
            else redPos.Y = 0;
        }
        if (down)
        {
            if (redPos.Y + blueSize.Height + frHeight / 50 < frHeight) redPos.Y += frHeight / 50;
            else redPos.Y = frHeight - redSize.Height;
        }
    }

    private void TimerListener(object sender, ElapsedEventArgs e)
    {
        MoveTails();
        
        //checking if the ball colides
        if (ballOutBlue)
        {
            if (ballPos.X + ballSize.Width < 0)
            {
                ballOutBlue = false;
                CreateNewRound();
            }
            else if (ballPos.X > frWidth / 2) ballOutBlue = false; //to make sure a certain bug doesn't happen 
        }
        else if (ballOutRed)
        {
            if ( ballPos.X > frWidth )
            {
                ballOutRed = false;
                CreateNewRound();
            }
            else if (ballPos.X < frWidth / 2) ballOutRed = false; //to make sure a certain bug doesn't happen

        }
        
        else if ( ballPos.X <= blueSize.Width )
        {
            if (ballPos.Y + ballSize.Height > bluePos.Y && ballPos.Y < bluePos.Y + blueSize.Height)
            {
                ballMoveDist = LogicalCourseAssign(bluePos.Y, 0.8f);
            }
            else ballOutBlue = true;
        }
        else if ( ballPos.X + ballSize.Width >= redPos.X )
        {
            if (ballPos.Y + ballSize.Height > redPos.Y && ballPos.Y < redPos.Y + redSize.Height)
            {
                ballMoveDist = LogicalCourseAssign(redPos.Y, 0.8f);
            }
            else ballOutRed = true;
        }
        
        if (ballPos.Y <= 0 || ballPos.Y + ballSize.Height >= frHeight)
            ballMoveDist.Y = -ballMoveDist.Y;
        
        //moving the ball
        ballPos = new(ballPos.X + ballMoveDist.X, ballPos.Y + ballMoveDist.Y);
        
        frame.panel.Refresh();
    }

    private void DelayTimerListener(object sender, ElapsedEventArgs e)
    {
        Console.WriteLine("gafre");
        delayTimer.Stop();
        timer.Start();
    }

}

static class Program
{
    static void Main()
    {
        BackEnd engine = new();
        engine.Run();
    }
}