﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Blish_HUD.Controls.Effects;
using Blish_HUD.Input;
using Newtonsoft.Json;

namespace Blish_HUD.Controls {

    [Flags]
    public enum CaptureType {
        None       = 0x0,
        Filter     = 0x1,
        Keyboard   = 0x2,
        Mouse      = 0x4,
        MouseWheel = 0x8,
        ForceNone  = 0x16
    }

    public abstract class Control : INotifyPropertyChanged, IDisposable {

        #region Static References

        #region Standards

        public static readonly DesignStandard ControlStandard = new DesignStandard(/*          Size */ Point.Zero,
                                                                                   /*   PanelOffset */ new Point(09, 28),
                                                                                   /* ControlOffset */ new Point(10, 10));

        public static class StandardColors {

            /// <summary>
            /// Color of standard text or untinted elements in an enabled control.
            ///
            /// The color is white (#FFFFFF).
            /// </summary>
            public static Color Default => new Color(0xffffffff);

            /// <summary>
            /// Color of standard text in a disabled control.
            ///
            /// The color is a dark gray (#AAAAAA).
            /// </summary>
            public static Color DisabledText => new Color(0xffaaaaaa);

            /// <summary>
            /// A tint often applied to control elements while the control is hovered over.
            ///
            /// The color is a light peach (#FFE4B5).
            /// </summary>
            public static Color Tinted => Color.FromNonPremultiplied(255, 228, 181, 255);

            /// <summary>
            /// Color of text or element shadows.
            ///
            /// The color is black (#000000).
            /// </summary>
            public static Color Shadow => new Color(0xff000000);

            /// <summary>
            /// Color of warning and alert text.  Also the color of lots of floating text.
            ///
            /// The color is yellow (#FFFF00).
            /// </summary>
            public static Color Yellow => Color.FromNonPremultiplied(255, 255, 0, 255);

            /// <summary>
            /// The color of error text.
            ///
            /// The color is red (#F20D13).
            /// </summary>
            public static Color Red => Color.FromNonPremultiplied(242, 13, 19, 255);

        }

        #endregion

        #region Resources

        private static readonly SpriteBatchParameters _defaultSpriteBatchParameters;

        #endregion


        private static readonly Tooltip _sharedTooltip;
        private static readonly Label   _sharedTooltipLabel;

        static Control() {
            _defaultSpriteBatchParameters = new SpriteBatchParameters();

            // Build shared tooltip
            _sharedTooltip = new Tooltip();
            _sharedTooltipLabel = new Label() {
                Text           = "Loading...",
                AutoSizeHeight = true,
                AutoSizeWidth  = true,
                ShowShadow     = true,
                Parent         = _sharedTooltip,
            };
        }

        #endregion

        #region Static Control Events

        public static event EventHandler<ControlActivatedEventArgs> ActiveControlChanged;

        private static Control _activeControl;
        public static Control ActiveControl {
            get => _activeControl;
            set {
                if (_activeControl == value) return;

                _activeControl = value;

                OnActiveControlChanged(new ControlActivatedEventArgs(_activeControl));
            }
        }

        private static void OnActiveControlChanged(ControlActivatedEventArgs e) {
            if (!string.IsNullOrEmpty(e.ActivatedControl?._basicTooltipText)) {
                _sharedTooltip.CurrentControl = e.ActivatedControl;
                _sharedTooltipLabel.Text      = e.ActivatedControl._basicTooltipText;
            }

            // TODO: _activeControl is probably not what should be passed as the sender...
            ActiveControlChanged?.Invoke(_activeControl, e);
        }

        #endregion

        #region Control Events

        #region Mouse Events

        public event EventHandler<MouseEventArgs> LeftMouseButtonPressed;
        public event EventHandler<MouseEventArgs> LeftMouseButtonReleased;
        public event EventHandler<MouseEventArgs> MouseMoved;
        public event EventHandler<MouseEventArgs> RightMouseButtonPressed;
        public event EventHandler<MouseEventArgs> RightMouseButtonReleased;
        public event EventHandler<MouseEventArgs> MouseWheelScrolled;
        public event EventHandler<MouseEventArgs> MouseEntered;
        public event EventHandler<MouseEventArgs> MouseLeft;

        /// <summary>
        /// Alias for <see cref="LeftMouseButtonReleased"/> with the difference that it only fires if <see cref="Enabled"/> is true.
        /// </summary>
        /// <remarks>Fires after <see cref="LeftMouseButtonReleased"/> fires.</remarks>
        public event EventHandler<MouseEventArgs> Click;

        protected virtual void OnLeftMouseButtonPressed(MouseEventArgs e) {
            this.LeftMouseButtonPressed?.Invoke(this, e);
        }

        protected virtual void OnLeftMouseButtonReleased(MouseEventArgs e) {
            this.LeftMouseButtonReleased?.Invoke(this, e);

            if (_enabled)
                OnClick(e);
        }

        protected virtual void OnMouseMoved(MouseEventArgs e) {
            this.MouseMoved?.Invoke(this, e);
        }

        protected virtual void OnRightMouseButtonPressed(MouseEventArgs e) {
            this.RightMouseButtonPressed?.Invoke(this, e);
        }

        protected virtual void OnRightMouseButtonReleased(MouseEventArgs e) {
            this.RightMouseButtonReleased?.Invoke(this, e);
        }

        protected virtual void OnMouseWheelScrolled(MouseEventArgs e) {
            this.MouseWheelScrolled?.Invoke(this, e);
        }

        protected virtual void OnMouseEntered(MouseEventArgs e) {
            this.MouseEntered?.Invoke(this, e);
        }

        protected virtual void OnMouseLeft(MouseEventArgs e) {
            this.MouseLeft?.Invoke(this, e);
        }

        /// <summary>
        /// Fires <see cref="OnLeftMouseButtonReleased"/> if the control is enabled.
        /// </summary>
        protected virtual void OnClick(MouseEventArgs e) {
            this.Click?.Invoke(this, e);
        }

        #endregion

        public event EventHandler<EventArgs> Shown;
        public event EventHandler<EventArgs> Hidden;
        public event EventHandler<ResizedEventArgs> Resized;
        public event EventHandler<MovedEventArgs> Moved;
        public event EventHandler<EventArgs> Disposed;

        protected virtual void OnShown(EventArgs e) {
            this.Shown?.Invoke(this, e);
        }

        protected virtual void OnHidden(EventArgs e) {
            this.Hidden?.Invoke(this, e);
        }

        protected virtual void OnResized(ResizedEventArgs e) {
            this.Resized?.Invoke(this, e);
        }

        protected virtual void OnMoved(MovedEventArgs e) {
            this.Moved?.Invoke(this, e);
        }

        #endregion

        protected bool _mouseOver = false;
        [JsonIgnore]
        public bool MouseOver {
            get => _mouseOver;
            private set {
                if (_mouseOver == value) return;

                _mouseOver = value;

                if (_mouseOver) {
                    OnMouseEntered(new MouseEventArgs(Input.MouseState));
                } else {
                    OnMouseLeft(new MouseEventArgs(Input.MouseState));
                }

                OnPropertyChanged();
            }
        }

        protected Point _location;
        public Point Location {
            get => _location;
            set {
                if (_location == value) return;

                var previousLocation = _location;

                _location = value;

                OnPropertyChanged();

                // We do this to make sure we raise PropertyChanged events for alias properties
                if (previousLocation.Y != _location.Y)
                    OnPropertyChanged(nameof(this.Top));
                if (previousLocation.X != _location.X)
                    OnPropertyChanged(nameof(this.Left));
                if (previousLocation.Y + _size.Y != _location.Y + _size.Y)
                    OnPropertyChanged(nameof(this.Bottom));
                if (previousLocation.X + _size.X != _location.X + _size.X)
                    OnPropertyChanged(nameof(this.Right));

                OnMoved(new MovedEventArgs(previousLocation, _location));
            }
        }

        #region Location Aliases

        [JsonIgnore]
        public int Top {
            get => _location.Y;
            set {
                if (_location.Y == value) return;

                this.Location = new Point(_location.X, value);
            }
        }

        [JsonIgnore]
        public int Right {
            get => _location.X + _size.X;
            set {
                if (value == _location.X + _size.X) return;

                this.Location = new Point(value - this.Width, _location.Y);
            }
        }

        [JsonIgnore]
        public int Bottom {
            get => _location.Y + _size.Y;
            set {
                if (value == _location.Y + _size.Y) return;

                this.Location = new Point(_location.X, value - this.Height);
            }
        }

        [JsonIgnore]
        public int Left {
            get => _location.X;
            set {
                if (value == _location.X) return;

                this.Location = new Point(value, _location.Y);
            }
        }

        #endregion
        
        protected Point _size = new Point(40, 20);
        /// <summary>
        /// The size of the control.  Both the X and Y component must be greater than 0.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either the X or Y component are 0 or less.</exception>
        public Point Size {
            get => _size;
            set {
                if (_size == value) return;

                // To render, the control must have positive dimensions
                if (_size.X <= 0 || _size.Y <= 0) return;
                    //throw new ArgumentOutOfRangeException($"{nameof(this.Size)} must have at least an area of 1 to render.");

                var previousSize = _size;

                _size = value;

                OnPropertyChanged();

                if (previousSize.Y != _size.Y)
                    OnPropertyChanged(nameof(this.Height), true);
                if (previousSize.X != _size.X)
                    OnPropertyChanged(nameof(this.Width), true);
                if (_location.Y + previousSize.Y != _location.Y + _size.Y)
                    OnPropertyChanged(nameof(this.Bottom), true);
                if (_location.X + previousSize.X != _location.X + _size.X)
                    OnPropertyChanged(nameof(this.Right), true);

                OnResized(new ResizedEventArgs(previousSize, _size));

                this.Invalidate();
            }
        }

        #region Size aliases

        [JsonIgnore]
        public int Width {
            get => _size.X;
            set {
                if (_size.X == value) return;

                this.Size = new Point(value, _size.Y);
            }
        }

        [JsonIgnore]
        public int Height {
            get => _size.Y;
            set {
                if (_size.Y == value) return;

                this.Size = new Point(_size.X, value);
            }
        }

        #endregion

        // TODO: Allow controls to skip clipping via a "ClipsBounds" setting
        public bool ClipsBounds { get; set; } = true;

        private ControlEffect _effectBehind;
        /// <summary>
        /// The <see cref="ControlEffect"/> to apply behind the control.
        /// </summary>
        protected ControlEffect EffectBehind {
            get => _effectBehind;
            set => SetProperty(ref _effectBehind, value);
        }

        private ControlEffect _effectInFront;
        /// <summary>
        /// [NOT IMPLEMENTED] The <see cref="ControlEffect"/> to apply on top of the control.
        /// </summary>
        protected ControlEffect EffectInFront {
            get => _effectInFront;
            set => SetProperty(ref _effectInFront, value);
        }

        /// <summary>
        /// The bounds of the control, relative to the parent control.
        /// </summary>
        [JsonIgnore]
        public Rectangle LocalBounds => new Rectangle(this.Location, this.Size);

        // TODO: Ensure that when properties AbsoluteBounds derives from change, this one also raises a PropertyChanged event
        /// <summary>
        /// The bounds of the control, relative to the overlay window / SpriteScreen.
        /// </summary>
        [JsonIgnore]
        public Rectangle AbsoluteBounds {
            get {
                if (_parent == null) return this.LocalBounds;

                // Clean this up
                // This is really the absolute bounds of the ContentRegion currently because mouse
                // input is currently using this to determine if the click was within the region
                return new Rectangle(_parent.AbsoluteBounds.X + _parent.ContentRegion.X + _location.X - _parent.HorizontalScrollOffset,
                                     _parent.AbsoluteBounds.Y + _parent.ContentRegion.Y + _location.Y - _parent.VerticalScrollOffset,
                                     _size.X,
                                     _size.Y);
            }
        }

        [JsonIgnore]
        public Point RelativeMousePosition => Input.MouseState.Position - this.AbsoluteBounds.Location;

        private ContextMenuStrip _menu;
        /// <summary>
        /// If provided, the menu will display when the control is right-clicked on.
        /// </summary>
        public ContextMenuStrip Menu {
            get => _menu;
            set => SetProperty(ref _menu, value);
        }

        private Tooltip _tooltip;
        /// <summary>
        /// If provided, the Tooltip will display when the mouse is over the control.
        /// Do not use this if you are already using <see cref="BasicTooltipText"/>.
        /// </summary>
        public Tooltip Tooltip {
            get => _tooltip;
            set => SetProperty(ref _tooltip, value);
        }

        private string _basicTooltipText;
        /// <summary>
        /// If provided, a tooltip will be shown with the provided text when the mouse is over the control.
        /// Do not use this if you are already using <see cref="Tooltip"/>.
        /// </summary>
        public string BasicTooltipText {
            get => _basicTooltipText;
            set {
                if (!SetProperty(ref _basicTooltipText, value)) return;

                // In the event that the tooltip text is changed while it's being shown, this will update it
                if (Control.ActiveControl == this) {
                    _sharedTooltipLabel.Text = value;
                }

                if (!string.IsNullOrEmpty(value)) {
                    this.Tooltip = _sharedTooltip;

                    this.MouseEntered += delegate {
                        _sharedTooltipLabel.Text = this.BasicTooltipText;
                    };
                } else {
                    this.Tooltip.Hide();
                    this.Tooltip = null;
                }
            }
        }

        protected float _opacity = 1f;
        public float Opacity {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }

        protected bool _visible = true;
        public bool Visible {
            get => _visible;
            set {
                if (SetProperty(ref _visible, value)) {
                    if (_visible) OnShown(EventArgs.Empty);
                    else OnHidden(EventArgs.Empty);
                }
            }
        }

        protected Thickness _padding = Thickness.Zero;
        public Thickness Padding {
            get => _padding;
            set => SetProperty(ref _padding, value, true);
        }

        private Container _parent;
        public Container Parent {
            get => _parent;
            set {
                var currentParent = _parent;

                if (SetProperty(ref _parent, value)) {
                    currentParent?.RemoveChild(this);

                    _parent = value;

                    if (this.Parent == null || !this.Parent.AddChild(this)) {
                        _parent = null;
                    }
                }
            }
        }

        protected int _zIndex = 5;
        public int ZIndex {
            get => _zIndex;
            set => SetProperty(ref _zIndex, value);
        }

        protected bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        protected Color _backgroundColor = Color.Transparent;
        [JsonIgnore]
        public Color BackgroundColor {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        #region Render Properties
        
        [JsonIgnore]
        internal LayoutState LayoutState { get; private set; } = LayoutState.SkipDraw;

        protected SpriteBatchParameters _spriteBatchParameters;

        [JsonIgnore]
        public SpriteBatchParameters SpriteBatchParameters {
            get => _spriteBatchParameters ?? _defaultSpriteBatchParameters;
            set {
                if (_spriteBatchParameters != _defaultSpriteBatchParameters)
                    _spriteBatchParameters = value;
            }
        }

        #endregion


        // TODO: Not sure if these are needed anymore since GameServices are much easier to reference now
        // Aliases to make life easier
        protected static ContentService   Content   => GameService.Content;
        protected static InputService     Input     => GameService.Input;
        protected static AnimationService Animation => GameService.Animation;
        protected static GraphicsService  Graphics  => GameService.Graphics;

        protected Control() {
            // TODO: This needs to get handled by the menustrip itself, not by the control
            /* They activate on the mouse down for the right-click menus which
               deviates from their norm (seems as if everything else activates on
               mouse button up - 🤷‍) */
            this.RightMouseButtonPressed += ActivateContextMenuStrip;
        }

        // TODO: This needs to be moved into the ContextMenuStrip class - the control itself shouldn't be doing this work
        private void ActivateContextMenuStrip(object sender, MouseEventArgs e) {
            if (this.Menu == null || !this.Enabled) return;

            /* We're going to assume nobody has a display so small that the ContextMenuStrip
               just can't fit in any direction */
            int topPos = Input.MouseState.Position.Y + this.Menu.Height > Graphics.SpriteScreen.Height
                             ? -this.Menu.Height
                             : 0;

            int leftPos = Input.MouseState.Position.X + this.Menu.Width < Graphics.SpriteScreen.Width
                              ? 0
                              : -this.Menu.Width;

            this.Menu.Location = Input.MouseState.Position + new Point(leftPos, topPos);

            this.Menu.Visible = true;
        }
        
        /// <summary>
        /// Avoid overriding <see cref="Invalidate"/> as it has the potential to be called multiple times prior to a render taking place.
        /// </summary>
        public virtual void Invalidate() {
            this.LayoutState = LayoutState.Invalidated;
        }

        /// <summary>
        /// Called whenever the size or location of the control is changed.  Is also called if <see cref="Invalidate"/> is called and should contain any calculations needed for the call to Draw the <see cref="Control"/>.
        /// </summary>
        public virtual void RecalculateLayout() {
            /* NOOP */
        }
        
        protected float AbsoluteOpacity(bool isInternal) {
            if (_parent == null) return _opacity;

            return isInternal
                       ? _parent.AbsoluteOpacity(true) - (1f - _opacity)
                       : MathHelper.Clamp(_parent.AbsoluteOpacity(true) - (1f - _opacity), 0f, 1f);
        }

        public float AbsoluteOpacity() {
            return AbsoluteOpacity(false);
        }

        /// <summary>
        /// Specifies which type of input this <see cref="Control"/> accepts, possibly blocks from other <see cref="Control"/>s, and prevents the game from seeing.
        /// </summary>
        public CaptureType Captures => CapturesInput();

        // TODO: Consider making CapturesInput abstract
        /// <summary>
        /// Override to specify which type of input this <see cref="Control"/> accepts.
        /// </summary>
        /// <seealso cref="Captures"/>
        protected virtual CaptureType CapturesInput() {
            return CaptureType.Filter;
        }

        public virtual void TriggerKeyboardInput(KeyboardMessage e) { /* NOOP */ }

        public virtual Control TriggerMouseInput(MouseEventType mouseEventType, MouseState ms) {
            var inputCapture = CapturesInput();

            if (inputCapture == CaptureType.None) return null;

            switch (mouseEventType) {
                case MouseEventType.LeftMouseButtonPressed:
                    OnLeftMouseButtonPressed(new MouseEventArgs(ms));
                    return this;
                    break;
                case MouseEventType.LeftMouseButtonReleased:
                    OnLeftMouseButtonReleased(new MouseEventArgs(ms));
                    return this;
                    break;
                case MouseEventType.RightMouseButtonPressed:
                    OnRightMouseButtonPressed(new MouseEventArgs(ms));
                    return this;
                    break;
                case MouseEventType.RightMouseButtonReleased:
                    OnRightMouseButtonReleased(new MouseEventArgs(ms));
                    return this;
                    break;
                case MouseEventType.MouseMoved:
                    OnMouseMoved(new MouseEventArgs(ms));
                    this.MouseOver = true;
                    return this;
                    break;
                case MouseEventType.MouseWheelScrolled:
                    OnMouseWheelScrolled(new MouseEventArgs(ms));
                    return inputCapture.HasFlag(CaptureType.MouseWheel) ? this : null;
                    break;
            }

            return null;
        }

        /// <summary>
        /// Makes the control visible.
        /// </summary>
        public virtual void Show() {
            this.Visible = true;
        }

        /// <summary>
        /// Hides the control so that it is no longer visible.
        /// </summary>
        public virtual void Hide() {
            this.Visible = false;
        }

        public virtual void DoUpdate(GameTime gameTime) { /* NOOP */ }

        public void Update(GameTime gameTime) {
            DoUpdate(gameTime);

            if (this.LayoutState != LayoutState.Ready) {
                RecalculateLayout();
                this.LayoutState = LayoutState.Ready;
            }

            CheckMouseLeft();
        }

        private void CheckMouseLeft() {
            if (_mouseOver && !this.AbsoluteBounds.Contains(Input.MouseState.Position)) {
                this.MouseOver = false;
            }
        }

        /// <summary>
        /// Draw the control.
        /// </summary>
        /// <param name="bounds">The draw region of the control.  Anything outside of this region will be clipped.  If this control is the child of a container, it could potentially be clipped even further by <see cref="spriteBatch.GraphicsDevice.ScissorRectangle" />.</param>
        protected abstract void Paint(SpriteBatch spriteBatch, Rectangle bounds);

        public virtual void Draw(SpriteBatch spriteBatch, Rectangle drawBounds, Rectangle scissor) {
            var controlScissor = Rectangle.Intersect(scissor, this.AbsoluteBounds.WithPadding(_padding)).ScaleBy(Graphics.UIScaleMultiplier);

            Graphics.GraphicsDevice.ScissorRectangle = controlScissor;

            this.EffectBehind?.Draw(spriteBatch, drawBounds);

            spriteBatch.Begin(this.SpriteBatchParameters);
                
            // Draw background
            if (_backgroundColor != Color.Transparent)
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, drawBounds, _backgroundColor);

            if (!this.ClipsBounds) {
                Graphics.GraphicsDevice.ScissorRectangle = Graphics.SpriteScreen.LocalBounds;
            }

            // Draw control
            Paint(spriteBatch, drawBounds);

            spriteBatch.End();

            //this.EffectInFront?.Draw(drawBounds);
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void DisposeControl() { /* NOOP */ }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // Let everything know we're disposing (esp. Data Bindings)
                    this.Disposed?.Invoke(this, EventArgs.Empty);

                    // Unassociate any subcontrols
                    this.EffectBehind  = null;
                    this.EffectInFront = null;

                    // Disconnect all existing event handlers
                    this.LeftMouseButtonPressed   = null;
                    this.LeftMouseButtonReleased  = null;
                    this.MouseMoved               = null;
                    this.RightMouseButtonPressed  = null;
                    this.RightMouseButtonReleased = null;
                    this.MouseWheelScrolled       = null;
                    this.MouseEntered             = null;
                    this.MouseLeft                = null;

                    this.Resized  = null;
                    this.Moved    = null;
                    this.Disposed = null;

                    // Cancel any animations that were currently running on this object
                    Animation.Tweener.TargetCancel(this);

                    // Remove self from parent object
                    this.Parent = null;

                    this.DisposeControl();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        #region Property Management and Binding

        protected bool SetProperty<T>(ref T property, T newValue, bool invalidateLayout = false, [CallerMemberName] string propertyName = null) {
            if (Equals(property, newValue) || propertyName == null) return false;

            property = newValue;

            OnPropertyChanged(propertyName, invalidateLayout);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName, bool invalidateLayout) {
            if (string.IsNullOrEmpty(propertyName)) return;

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (invalidateLayout) {
                //Console.WriteLine($"[INVALIDATED LAYOUT] {this.GetType().Name} > {propertyName}");
                Invalidate();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            OnPropertyChanged(propertyName, false);
        }

        #endregion

    }
}
