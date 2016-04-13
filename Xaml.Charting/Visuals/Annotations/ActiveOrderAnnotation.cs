using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Annotations {
    public class ActiveOrderAnnotation : AnnotationBase {
        #region dependency properties

        public static readonly DependencyProperty OrderTextProperty = DependencyProperty.Register("OrderText", typeof(string), typeof(ActiveOrderAnnotation), new PropertyMetadata(null));
        public static readonly DependencyProperty OrderSizeTextProperty = DependencyProperty.Register("OrderSizeText", typeof(string), typeof(ActiveOrderAnnotation), new PropertyMetadata(null));
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(ActiveOrderAnnotation), new PropertyMetadata(Brushes.White));
        public static readonly DependencyProperty CancelButtonFillProperty = DependencyProperty.Register("CancelButtonFill", typeof(Brush), typeof(ActiveOrderAnnotation), new PropertyMetadata(Brushes.DarkGray));
        public static readonly DependencyProperty CancelButtonColorProperty = DependencyProperty.Register("CancelButtonColor", typeof(Brush), typeof(ActiveOrderAnnotation), new PropertyMetadata(Brushes.Black));
        public static readonly DependencyProperty YDragStepProperty = DependencyProperty.Register("YDragStep", typeof(double), typeof(ActiveOrderAnnotation), new PropertyMetadata(0d));
        public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register("IsAnimationEnabled", typeof(bool), typeof(ActiveOrderAnnotation), new PropertyMetadata(true));
        public static readonly DependencyProperty OrderErrorTextProperty = DependencyProperty.Register("OrderErrorText", typeof(string), typeof(ActiveOrderAnnotation), new PropertyMetadata("ERROR"));
        public static readonly DependencyProperty BlinkColorProperty = DependencyProperty.Register("BlinkColor", typeof(Color), typeof(ActiveOrderAnnotation), new PropertyMetadata(Colors.Black));

        public string OrderText
        {
            get { return (string)GetValue(OrderTextProperty); }
            set { SetValue(OrderTextProperty, value); }
        }

        public string OrderSizeText
        {
            get { return (string)GetValue(OrderSizeTextProperty); }
            set { SetValue(OrderSizeTextProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public Brush CancelButtonFill
        {
            get { return (Brush)GetValue(CancelButtonFillProperty); }
            set { SetValue(CancelButtonFillProperty, value); }
        }

        public Brush CancelButtonColor
        {
            get { return (Brush)GetValue(CancelButtonColorProperty); }
            set { SetValue(CancelButtonColorProperty, value); }
        }

        public double YDragStep
        {
            get { return (double)GetValue(YDragStepProperty); }
            set { SetValue(YDragStepProperty, value); }
        }

        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
        }

        public string OrderErrorText
        {
            get { return (string)GetValue(OrderErrorTextProperty); }
            set { SetValue(OrderErrorTextProperty, value); }
        }

        public Color BlinkColor
        {
            get { return (Color)GetValue(BlinkColorProperty); }
            set { SetValue(BlinkColorProperty, value); }
        }

        #endregion

        public event Action<ActiveOrderAnnotation> CancelClick;

        private Line _line;
        private Grid _gridOrderInfo;
        private Button _cancelButton;
        private Border _borderOrderCount, _borderOrderText;
        private Polygon _orderPointer;
        private TextBlock _txtOrderText, _txtCount;
        readonly AxisMarkerAnnotation _axisMarker;

        private bool _templateInitialized;

        Storyboard _fillAnimation, _errorAnimation;

        public ActiveOrderAnnotation()
        {
            DefaultStyleKey = typeof (ActiveOrderAnnotation);

            _axisMarker = new AxisMarkerAnnotation
            {
                IsEditable = false,
                IsSelected = false,
            };

            _axisMarker.SetBindings(VisibilityProperty, this, nameof(Visibility), BindingMode.OneWay);
            _axisMarker.SetBindings(ForegroundProperty, this, nameof(Foreground), BindingMode.OneWay);
            _axisMarker.SetBindings(BackgroundProperty, this, nameof(Background), BindingMode.OneWay);
            _axisMarker.SetBindings(BorderBrushProperty, this, nameof(Background), BindingMode.OneWay);
            _axisMarker.SetBindings(XAxisIdProperty, this, nameof(XAxisId), BindingMode.OneWay);
            _axisMarker.SetBindings(YAxisIdProperty, this, nameof(YAxisId), BindingMode.OneWay);
            _axisMarker.SetBindings(Y1Property, this, nameof(Y1), BindingMode.OneWay);
            _axisMarker.SetBindings(IsHiddenProperty, this, nameof(IsHidden), BindingMode.OneWay);
        }

        protected override void HandleIsEditable()
        {
            var usedCursor = IsEditable ? Cursors.SizeNS : Cursors.Arrow;
            _gridOrderInfo?.SetValue(CursorProperty, usedCursor);
        }

        protected override Cursor GetSelectedCursor()
        {
            return Cursors.Arrow;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AnnotationRoot = GetAndAssertTemplateChild<Grid>("PART_AnnotationRoot");
            _line = GetAndAssertTemplateChild<Line>("PART_Line");
            _gridOrderInfo = GetAndAssertTemplateChild<Grid>("PART_GridOrderInfo");
            _borderOrderCount = GetAndAssertTemplateChild<Border>("PART_GridOrderCount");
            _borderOrderText = GetAndAssertTemplateChild<Border>("PART_GridOrderText");
            _orderPointer = GetAndAssertTemplateChild<Polygon>("PART_OrderPointer");
            _txtCount = GetAndAssertTemplateChild<TextBlock>("PART_OrderCountText");
            _txtOrderText = GetAndAssertTemplateChild<TextBlock>("PART_OrderText");

            if (_cancelButton == null)
            {
                _cancelButton = GetAndAssertTemplateChild<Button>("PART_CancelButton");
                _cancelButton.Click += (sender, args) => CancelClick?.Invoke(this);

                _cancelButton.SetBindings(IsEnabledProperty, this, nameof(IsEditable), BindingMode.OneWay);
            }

            _templateInitialized = true;

            HandleIsEditable();
            Refresh();
        }

        public override void OnAttached()
        {
            base.OnAttached();

            _axisMarker.Services = Services;
            _axisMarker.ParentSurface = ParentSurface;
            _axisMarker.IsAttached = true;
            _axisMarker.OnAttached();
        }

        public override void OnDetached()
        {
            base.OnDetached();

            _axisMarker.OnDetached();
            _axisMarker.Services = null;
            _axisMarker.ParentSurface = null;
            _axisMarker.IsAttached = false;
        }

        public override void Update(ICoordinateCalculator<double> xCoordinateCalculator, ICoordinateCalculator<double> yCoordinateCalculator) {
            base.Update(xCoordinateCalculator, yCoordinateCalculator);
            _axisMarker.Update(xCoordinateCalculator, yCoordinateCalculator);
        }

        protected override void GetPropertiesFromIndex(int index, out DependencyProperty X, out DependencyProperty Y)
        {
            X = X1Property;
            Y = Y1Property;
        }

        protected override void SetBasePoint(Point newPoint, int index, IAxis xAxis, IAxis yAxis)
        {
            var canvas = GetCanvas(AnnotationCanvas);

            var dataValues = FromCoordinates(newPoint);
            var newX = dataValues[0];

            DependencyProperty X, Y;
            GetPropertiesFromIndex(index, out X, out Y);

            if(IsCoordinateValid(newPoint.X, canvas.ActualWidth))
                SetCurrentValue(X, newX);
        }

        protected override void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizOffset, double vertOffset)
        {
            var axis = YAxis;
            var canvas = GetCanvas(AnnotationCanvas);

            // Compute new coordinates in pixels
            var y1 = coordinates.Y1Coord + vertOffset;

            // If any are out of bounds ... 
            if (!IsCoordinateValid(y1, canvas.ActualHeight))
            {
                // Clip to bounds
                if (y1 < 0) vertOffset -= y1 - 1;
                if (y1 > canvas.ActualHeight) vertOffset -= y1 - (canvas.ActualHeight - 1);

                // Reassign
                y1 = coordinates.Y1Coord + vertOffset;
            }

            if(YDragStep > 0)
            {
                var dragStartCoord = FromCoordinate(coordinates.Y1Coord, axis);
                var newValue = FromCoordinate(y1, axis);

                var diff = Math.Abs(dragStartCoord.ToDouble() - newValue.ToDouble());
                var times = (int) Math.Round(diff/YDragStep);

                var sign = !axis.FlipCoordinates ? -Math.Sign(vertOffset) : Math.Sign(vertOffset);

                var expectedValue = dragStartCoord.ToDouble() + sign*times*YDragStep;

                y1 = ToCoordinate(expectedValue, axis);
                vertOffset = y1 - coordinates.Y1Coord;
            }

            if(IsCoordinateValid(y1, canvas.ActualHeight))
            {
                var point = new Point {X = coordinates.X1Coord, Y = y1};

                base.SetBasePoint(point, 0, XAxis, YAxis);

                OnAnnotationDragging(new AnnotationDragDeltaEventArgs(0, vertOffset));
            }
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if(XAxis != null && XAxis.IsPolarAxis)
                throw new InvalidOperationException("Polar axis is not supported for this type of annotation");

            return new CartesianAnnotationPlacementStrategy(this);
        }

        #region order animations

        public event Action<ActiveOrderAnnotation> AnimationDone;

        public void AnimateOrderFill()
        {
            if(IsAnimationEnabled)
                GetFillAnimation()?.Begin(this, true);
            else
                TryInvokeAnimationDone();
        }

        public void AnimateError()
        {
            if(IsAnimationEnabled)
                GetErrorAnimation()?.Begin(this, true);
            else
                TryInvokeAnimationDone();
        }

        Storyboard GetFillAnimation() {
            if(!_templateInitialized)
                return null;

            var altColor = BlinkColor;

            if (_fillAnimation != null)
            {
                ((ColorAnimation)_fillAnimation.Children[2]).From = altColor;
                ((ColorAnimation)_fillAnimation.Children[3]).From = altColor;
                ((ColorAnimation)_fillAnimation.Children[4]).From = altColor;
                return _fillAnimation;
            }

            _fillAnimation = new Storyboard();

            _fillAnimation.Completed += (sender, args) => TryInvokeAnimationDone();

            var animX = InitAnimation<DoubleAnimation>(_fillAnimation, _borderOrderCount, "RenderTransform.ScaleX");
            var animY = InitAnimation<DoubleAnimation>(_fillAnimation, _borderOrderCount, "RenderTransform.ScaleY");
            var colAnim1 = InitAnimation<ColorAnimation>(_fillAnimation, _borderOrderText, "Background.Color");
            var colAnim2 = InitAnimation<ColorAnimation>(_fillAnimation, _borderOrderCount, "Background.Color");
            var colAnim3 = InitAnimation<ColorAnimation>(_fillAnimation, _orderPointer, "Fill.Color");

            animX.To = animY.To = 1.5d;
            animX.Duration = animY.Duration = TimeSpan.FromMilliseconds(75);
            animX.EasingFunction = animY.EasingFunction = new ExponentialEase();

            colAnim1.RepeatBehavior = colAnim2.RepeatBehavior = colAnim3.RepeatBehavior = new RepeatBehavior(3);
            colAnim1.From = colAnim2.From = colAnim3.From = altColor;
            colAnim1.Duration = colAnim2.Duration = colAnim3.Duration = TimeSpan.FromMilliseconds(100);
            colAnim1.EasingFunction = colAnim2.EasingFunction = colAnim3.EasingFunction = new ExponentialEase
            {
                EasingMode = EasingMode.EaseIn,
                Exponent = 3
            };

            return _fillAnimation;
        }

        Storyboard GetErrorAnimation() {
            if(_errorAnimation != null || !_templateInitialized)
                return _errorAnimation;

            _errorAnimation = new Storyboard {FillBehavior = FillBehavior.Stop};

            _errorAnimation.Completed += (sender, args) => TryInvokeAnimationDone();

            var color1 = Colors.Red;
            var color2 = Colors.Black;

            InitErrorColorAnimation(_errorAnimation, _borderOrderText, "Background.Color", color1, color2);
            InitErrorColorAnimation(_errorAnimation, _borderOrderCount, "Background.Color", color1, color2);
            InitErrorColorAnimation(_errorAnimation, _orderPointer, "Fill.Color", color1, color2);
            InitErrorColorAnimation(_errorAnimation, _txtCount, "Foreground.Color", color2, color1);
            InitErrorColorAnimation(_errorAnimation, _txtOrderText, "Foreground.Color", color2, color1);

            var a = InitAnimation<StringAnimationUsingKeyFrames>(_errorAnimation, _txtOrderText, "Text");
            a.FillBehavior = FillBehavior.HoldEnd;

            a.KeyFrames.Add(new DiscreteStringKeyFrame {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                Value = OrderErrorText,
            });

            return _errorAnimation;
        }

        T InitAnimation<T>(Storyboard sb, DependencyObject target, string path) where T : AnimationTimeline, new() {
            var a = new T {
                AutoReverse = true,
                FillBehavior = FillBehavior.Stop,
            };

            Storyboard.SetTarget(a, target);
            Storyboard.SetTargetProperty(a, new PropertyPath(path));

            sb.Children.Add(a);

            return a;
        }

        void InitErrorColorAnimation(Storyboard sb, DependencyObject target, string path, Color col1, Color col2) {
            var a = InitAnimation<ColorAnimationUsingKeyFrames>(sb, target, path);

            a.RepeatBehavior = new RepeatBehavior(5);
            a.Duration = TimeSpan.FromMilliseconds(100);

            a.KeyFrames.Add(new DiscreteColorKeyFrame {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)),
                Value = col1,
            });
            a.KeyFrames.Add(new DiscreteColorKeyFrame {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50)),
                Value = col2,
            });
        }

        void TryInvokeAnimationDone()
        {
            var errAnimState = _errorAnimation.Return(a => a.GetCurrentState(this), ClockState.Stopped);
            var fillAnimState = _fillAnimation.Return(a => a.GetCurrentState(this), ClockState.Stopped);

            if (!IsAnimationEnabled || (errAnimState != ClockState.Active && fillAnimState != ClockState.Active))
                AnimationDone?.Invoke(this);
        }

        #endregion

        class CartesianAnnotationPlacementStrategy : CartesianAnnotationPlacementStrategyBase<ActiveOrderAnnotation> {
            public CartesianAnnotationPlacementStrategy(ActiveOrderAnnotation annotation) : base(annotation) { }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates) {
                var canvas = Annotation.GetCanvas(Annotation.AnnotationCanvas);
                
                var y = coordinates.Y1Coord;

                if(!y.IsRealNumber() || canvas == null)
                    return;

                var root = (Grid)Annotation.AnnotationRoot;
                var halfHeight = root.ActualHeight / 2;
                var lineSize = Math.Max(10, canvas.ActualWidth - coordinates.X1Coord);

                var line = Annotation._line;

                if (!line.X1.DoubleEquals(0) || !line.X2.DoubleEquals(lineSize) || !line.Y1.DoubleEquals(halfHeight) || !line.Y2.DoubleEquals(halfHeight))
                {
                    line.X1 = 0;
                    line.X2 = lineSize;
                    line.Y1 = Annotation._line.Y2 = halfHeight;
                    line.UpdateLayout(); // todo other way?
                }

                var yPos = y - halfHeight;

                var x = canvas.ActualWidth - Annotation.ActualWidth;

                Annotation.SetValue(Canvas.LeftProperty, x);
                Annotation.SetValue(Canvas.TopProperty, yPos);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return new[] { new Point(coordinates.X1Coord, coordinates.Y1Coord) };
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas) {
                var outOfBounds = coordinates.Y1Coord < 0 || coordinates.Y1Coord > canvas.ActualHeight || coordinates.X1Coord > canvas.ActualWidth;

                return !outOfBounds;
            }
        }
    }
}
