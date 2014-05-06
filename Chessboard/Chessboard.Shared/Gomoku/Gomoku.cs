using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ChessboardApp.Gomoku
{
    public enum Side
    {
        Black,
        White
    }

    struct MovePosition
    {
        public int x;
        public int y;
    }

    class Chessboard
    {
        public struct CheckResult
        {
            public bool connected;
            public Move[] connectedMoves;
            public int liveThree;
            public int breakThree;
            public int liveFour;
            public int breakFour;
            public int deadFour;
        }

        public Panel canvas;
        public int space;

        public double StoneSize
        {
            get;
            private set;
        }

        public Move[,] moves;

        public Chessboard(Panel canvas, int space)
        {
            this.canvas = canvas;
            this.space = space;
            this.StoneSize = canvas.Width / (space + 1);
            this.moves = new Move[space, space];
        }

        public CheckResult CheckConnected(Side checkSide)
        {
            CheckResult result = new CheckResult();

            for(int y = 0; y < space; y++)
            {
                for(int x = 0; x < space; x++)
                {
                    Move currentMove = moves[y, x];

                    if(currentMove == null || currentMove.side != checkSide)
                        continue;

                    //try to connect in possible 4 directions
                    int[,] directions = new int[,] { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };
                    for (int d = 0; d < 4; d++)
                    {
                        bool opened = !IsOutOfBound(x - directions[d, 0], y - directions[d, 1]) &&
                            moves[y - directions[d, 1], x - directions[d, 0]] == null;
                        bool blocked = false;
                        for (int c = 1; c <= 4; c++)
                        {
                            int newX = x + directions[d, 0] * c;
                            int newY = y + directions[d, 1] * c;
                            if (IsOutOfBound(newX, newY))
                            {
                                //blocked by the borders
                                blocked = true;
                                continue;
                            }
                            if (moves[newY, newX] == null)
                            {
                                //connected to a space, check for states
                                if(opened)
                                {
                                    if (c == 3)
                                        result.liveThree++;
                                    if (c == 4)
                                        result.liveFour++;
                                    //Search for break 3 and break 4
                                }
                                else
                                {
                                    if (c == 4)
                                        result.deadFour++;
                                    else
                                    {
                                        //Search for break 4
                                        bool breakFour = true;
                                        int leftToBreakFour = 4 - c;
                                        for (int breakC = 1; breakC <= leftToBreakFour; breakC++ )
                                        {
                                            int newX2 = x + directions[d, 0] * (c + breakC);
                                            int newY2 = y + directions[d, 1] * (c + breakC);
                                            if (IsOutOfBound(newX2, newY2) || moves[newY2, newX2] == null || moves[newY2, newX2].side != checkSide)
                                            {
                                                breakFour = false;
                                                break;
                                            }
                                        }
                                        if (breakFour)
                                            result.breakFour++;
                                    }
                                }
                                blocked = true;
                                break;
                            }
                            else if(moves[newY, newX].side != checkSide)
                            {
                                //blocked by opponent's stone
                                if (opened && c == 4)
                                    result.deadFour++;

                                blocked = true;
                                break;
                            }
                        }
                        if(!blocked)
                        {
                            //connected
                            result.connected = true;
                            result.connectedMoves = new Move[]{
                                moves[y,x],
                                moves[y + directions[d,1] * 1, x + directions[d,0] * 1],
                                moves[y + directions[d,1] * 2, x + directions[d,0] * 2],
                                moves[y + directions[d,1] * 3, x + directions[d,0] * 3],
                                moves[y + directions[d,1] * 4, x + directions[d,0] * 4]};
                            return result;
                        }
                    }
                }
            }
            //No connection
            result.connected = false;
            return result;
        }

        public void Remove(Move target)
        {
            moves[target.movePosition.y, target.movePosition.x] = null;
            canvas.Children.Remove(target.elipse);
            GC.Collect();
        }

        private bool IsOutOfBound(int x, int y)
        {
            return (x < 0 || y < 0 || x >= space || y >= space);
        }
    }

    class Move
    {
        public Side side;
        public Ellipse elipse;
        public MovePosition movePosition;
        public Move lastMove;

        private static SolidColorBrush blackBrush = new SolidColorBrush(Windows.UI.Colors.Black);
        private static SolidColorBrush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);

        public Move(Side side, MovePosition movePosition, Move lastMove, Chessboard chessBoard)
        {
            this.side = side;
            this.movePosition = movePosition;
            this.lastMove = lastMove;

            elipse = new Ellipse();
            elipse.Fill = side == Side.Black ? blackBrush : whiteBrush;
            elipse.Width = elipse.Height = chessBoard.StoneSize;
            elipse.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            elipse.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
            elipse.Margin = new Windows.UI.Xaml.Thickness((movePosition.x + 0.5) * chessBoard.StoneSize, (movePosition.y + 0.5) * chessBoard.StoneSize, 0, 0);
            elipse.Transitions = new Windows.UI.Xaml.Media.Animation.TransitionCollection();
            elipse.Transitions.Add(new Windows.UI.Xaml.Media.Animation.PopupThemeTransition());

            chessBoard.canvas.Children.Add(elipse);

            this.Highlight();
            if (lastMove != null)
                lastMove.elipse.Stroke = null;

            chessBoard.moves[movePosition.y, movePosition.x] = this;
        }

        public void Highlight()
        {
            elipse.Stroke = side == Side.Black ? whiteBrush : blackBrush;
        }
    }
}
