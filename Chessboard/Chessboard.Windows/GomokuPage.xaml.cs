using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI.Xaml.Shapes;
using Windows.UI.Input;
using Windows.UI.Popups;

// 空白頁項目範本已記錄在 http://go.microsoft.com/fwlink/?LinkId=234238

namespace ChessboardApp.Gomoku
{
    public sealed partial class GomokuPage : Page
    {
        private Chessboard chessboard;
        private Ellipse previewStone;
        private SolidColorBrush PreviewBrushRed = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 255, 0, 0));
        private SolidColorBrush PreviewBrushWhite = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 255, 255, 255));
        private SolidColorBrush PreviewBrushBlack = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 0, 0, 0));
        private SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Black);
        private SolidColorBrush lineBrushHighlighted = new SolidColorBrush(Windows.UI.Color.FromArgb(128,255,255,255));
        private Line[] horizontalLines;
        private Line[] verticalLines;
        private Line[] lastHightlightedLines;
        private Side turn = Side.Black;
        private Move lastMove;
        private bool isGameOver;
        

        public Side Turn
        {
            get { return turn; }
            private set 
            {
                turn = value;
                textBlock_turn.Text = textBlock_turn_right.Text = turn == Side.Black ? "黑子 回合" : "白子 回合"; 
            }
        }

        public GomokuPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Setup canvas sizes
            double canvasWidth = Window.Current.Bounds.Height;
            canvas.Width = canvas.Height = canvasWidth;

            InitGame();
        }

        private void InitGame()
        {
            //Setup chessBoard
            chessboard = new Chessboard(canvas, (int)slider_chessboard_space.Value);

            //Prepare preview stones
            previewStone = new Ellipse();
            previewStone.Width = previewStone.Height = chessboard.StoneSize;
            previewStone.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            previewStone.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
            previewStone.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            previewStone.Transitions = new Windows.UI.Xaml.Media.Animation.TransitionCollection();
            previewStone.Transitions.Add(new Windows.UI.Xaml.Media.Animation.EntranceThemeTransition());

            canvas.Children.Add(previewStone);

            //Draw borders
            DrawBorders();

            Turn = Side.Black;
            isGameOver = false;
        }

        private void ResetGame()
        {
            lastMove = null;
            chessboard = null;
            previewStone = null;
            horizontalLines = null;
            verticalLines = null;
            lastHightlightedLines = null;

            canvas.Children.Clear();

            textBlock_state.Text = textBlock_state_right.Text = "";

            GC.Collect();

            InitGame();
        }

        private async void Move(MovePosition movePosition)
        {
            if (movePosition.x < chessboard.space && movePosition.y < chessboard.space)
            {
                if (chessboard.moves[movePosition.y, movePosition.x] == null)
                {
                    lastMove = new Move(Turn, movePosition, lastMove, chessboard);

                    Chessboard.CheckResult result = chessboard.CheckConnected(Turn);

                    if (result.connected)
                    {
                        isGameOver = true;

                        MessageDialog dialog = new MessageDialog("遊戲結束");
                        dialog.Commands.Add(new UICommand("重新棋局", CommandInvokedHandler, 0));
                        dialog.Commands.Add(new UICommand("回到棋盤", CommandInvokedHandler, 1));
                        await dialog.ShowAsync();
                    }
                    else
                    {
                        string state = string.Empty;

                        if(result.liveThree > 0)
                            state += (result.liveThree > 1 ? result.liveThree.ToString() : "") + "活三\n";
                        if(result.liveFour > 0)
                            state += (result.liveFour > 1 ? result.liveFour.ToString() : "") + "活四\n";
                        /*if (result.breakFour > 0)
                            state += (result.breakFour > 1 ? result.breakFour.ToString() : "") + "斷四\n";*/
                        if (result.deadFour > 0)
                            state += (result.deadFour > 1 ? result.deadFour.ToString() : "") + "死四\n";

                        textBlock_state.Text = textBlock_state_right.Text = state;
                        Turn = Turn == Side.Black ? Side.White : Side.Black;
                    }
                }
            }
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            if(command.Id.Equals(0))
            {
                ResetGame();
            }
        }

        private void DrawBorders()
        {
            horizontalLines = new Line[chessboard.space];
            verticalLines = new Line[chessboard.space];

            double margin = chessboard.StoneSize;
            for (int i = 0; i < chessboard.space; i++ )
            {
                double x = (i+1) * margin;
                Line horizontalLine = DrawLine(margin, x, chessboard.canvas.Width - margin, x);
                Line verticalLine = DrawLine(x, margin, x, chessboard.canvas.Width - margin);
                horizontalLines[i] = horizontalLine;
                verticalLines[i] = verticalLine;
            }
        }

        private Line DrawLine(double x1, double y1, double x2, double y2)
        {
            Line line = new Line();
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = x2;
            line.Y2 = y2;
            line.Stroke = lineBrush;
            line.StrokeThickness = 1.5;

            canvas.Children.Add(line);

            return line;
        }

        private void canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (isGameOver)
                return;

            Point cursorPosition = e.GetCurrentPoint(canvas).Position;
            MovePosition movePosition = GetMovePosition(cursorPosition);
            Point correctedPosition = GetCorrectedPosition(movePosition);

            if (movePosition.x < chessboard.space && movePosition.y < chessboard.space)
            {
                previewStone.Fill = chessboard.moves[movePosition.y, movePosition.x] != null ? PreviewBrushRed : (Turn == Side.Black ? PreviewBrushBlack : PreviewBrushWhite);
                previewStone.Margin = new Thickness(correctedPosition.X, correctedPosition.Y, 0, 0);
                //Show preview stone
                previewStone.Visibility = Windows.UI.Xaml.Visibility.Visible;

                //Show preview highlighted line for touch users
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
                {
                    if (lastHightlightedLines == null)
                    {
                        lastHightlightedLines = new Line[2];
                    }
                    else
                    {
                        //Resume last highlighted lines
                        lastHightlightedLines[0].Stroke = lineBrush;
                        lastHightlightedLines[1].Stroke = lineBrush;

                    }
                    //Highlight new lines
                    lastHightlightedLines[0] = horizontalLines[movePosition.y];
                    lastHightlightedLines[1] = verticalLines[movePosition.x];

                    lastHightlightedLines[0].Stroke = lineBrushHighlighted;
                    lastHightlightedLines[1].Stroke = lineBrushHighlighted;
                }
            }
            else
            {
                //Cursor is out of chessboard
                previewStone.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isGameOver)
                return;

            Point cursorPosition = e.GetCurrentPoint(canvas).Position;
            MovePosition movePosition = GetMovePosition(cursorPosition);
            Point correctedPosition = GetCorrectedPosition(movePosition);

            if (movePosition.x < chessboard.space && movePosition.y < chessboard.space)
            {
                previewStone.Fill = chessboard.moves[movePosition.y, movePosition.x] != null ? PreviewBrushRed : (Turn == Side.Black ? PreviewBrushBlack : PreviewBrushWhite);
                previewStone.Margin = new Thickness(correctedPosition.X, correctedPosition.Y, 0, 0);
                //Show preview stone
                previewStone.Visibility = Windows.UI.Xaml.Visibility.Visible;

                //Show preview highlighted line for touch users
                if(e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
                {
                    if (lastHightlightedLines == null)
                    {
                        lastHightlightedLines = new Line[2];
                    }
                    else
                    {
                        //Resume last highlighted lines
                        lastHightlightedLines[0].Stroke = lineBrush;
                        lastHightlightedLines[1].Stroke = lineBrush;

                    }
                    //Highlight new lines
                    lastHightlightedLines[0] = horizontalLines[movePosition.y];
                    lastHightlightedLines[1] = verticalLines[movePosition.x];

                    lastHightlightedLines[0].Stroke = lineBrushHighlighted;
                    lastHightlightedLines[1].Stroke = lineBrushHighlighted;
                }
            }
            else
            {
                //Cursor is out of chessboard
                previewStone.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            
        }

        private void canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isGameOver)
                return;

            //Hide preview stone
            previewStone.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //Resume highlighted lines
            if(lastHightlightedLines != null)
            {
                //Resume last highlighted lines
                lastHightlightedLines[0].Stroke = lineBrush;
                lastHightlightedLines[1].Stroke = lineBrush;
                lastHightlightedLines = null;
            }

            Point position = e.GetCurrentPoint(canvas).Position;
            MovePosition movePosition = GetMovePosition(position);

            Move(movePosition);
        }

        private void canvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (isGameOver)
                return;

            //Hide preview stone
            previewStone.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //Resume highlighted lines
            if (lastHightlightedLines != null)
            {
                //Resume last highlighted lines
                lastHightlightedLines[0].Stroke = lineBrush;
                lastHightlightedLines[1].Stroke = lineBrush;
                lastHightlightedLines = null;
            }
        }

        private MovePosition GetMovePosition(Point cursorPosition)
        {
            MovePosition movePosition = new MovePosition();
            movePosition.x = (int)((cursorPosition.X - chessboard.StoneSize / 2) / chessboard.StoneSize);
            movePosition.y = (int)((cursorPosition.Y - chessboard.StoneSize / 2) / chessboard.StoneSize);
            return movePosition;
        }

        private Point GetCorrectedPosition(MovePosition movePosition)
        {
            double x = (movePosition.x + 0.5) * chessboard.StoneSize;
            double y = (movePosition.y + 0.5) * chessboard.StoneSize;
            return new Point(x,y);
        }

        private void button_restart_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void button_undo_Click(object sender, RoutedEventArgs e)
        {
            if (lastMove == null)
                return;

            Move undoMove = lastMove;
            lastMove = undoMove.lastMove;

            chessboard.Remove(undoMove);

            //hightlight last move and switch turn
            textBlock_state.Text = "";
            Turn = Turn == Side.Black ? Side.White : Side.Black;
            if(lastMove != null)
                lastMove.Highlight();

            if(isGameOver)
                isGameOver = false;
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (slider_chessboard_space != null && slider_chessboard_space_right != null)
                if ((int)e.NewValue % 2 == 0)
                    slider_chessboard_space.Value = slider_chessboard_space_right.Value = e.NewValue + 1;
        }

        
    }
}
