using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace MagicBalloons
{
	public partial class MainWindow : Window
	{
		private static readonly int MAX_BALLOONS = 15;

		private Screen[] UsersScreens { get; } = Screen.AllScreens;
		private double WindowWidth { get; } = 0;
		private List<Image> BalloonImages { get; } = new List<Image>();
		private List<int> BalloonSpeeds { get; } = new List<int>();
		private Random Rnd { get; } = new Random();

		private bool stopTimer;
		private DispatcherTimer balloonTimer;
		private DispatcherTimer movementTimer;

		/// <summary>
		/// Initialise the program and set up the bounds that the balloons will be disaplayed on.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();
			double minimumLeftPosition = 0;
			double maximumScreenHeight = 1080;

			// set up the window so that it is the length of all screens and height of the largest screen.
			for (int i = 0; i < UsersScreens.Length; i++) {
				System.Drawing.Rectangle screenBounds = UsersScreens[i].WorkingArea;
				WindowWidth += screenBounds.Width;

				// Check if this screen is the *Left Most* screen
				if (i == 0 || screenBounds.Left < minimumLeftPosition) {
					minimumLeftPosition = screenBounds.Left;
				}
				// Check if this scren has the highest *Height* resolution
				if (i == 0 || screenBounds.Height > maximumScreenHeight) {
					maximumScreenHeight = screenBounds.Height;
				}
			} // End For

			// Initalize the window parameters
			this.Left = minimumLeftPosition;
			this.Top = 0;
			this.Width = WindowWidth;
			this.Height = maximumScreenHeight;
		}


		/// <summary>
		/// Window Load event.
		/// This removes the program from the taskbar and alt-tab menu so that it does not get in the way and can't be seen. 
		/// This will also start the timer for balloons.
		/// </summary>
		/// <param name="sender">Window</param>
		/// <param name="e">Load event args</param>
		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			WindowInteropHelper wndHelper = new WindowInteropHelper(this);
			int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
			exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
			SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

			// Start the loop for showing the baloons
			balloonTimer = new DispatcherTimer();
			balloonTimer.Tick += CheckTimeAndShowBalloons;
			balloonTimer.Interval = new TimeSpan(0, 0, 1);
			balloonTimer.Start();
		}

		/// <summary>
		/// Check what the time is and show the balloons if it the current seconds are 25 or 55.
		/// </summary>
		/// <param name="sender">DispatcherTimer</param>
		/// <param name="e">The event args</param>
		private void CheckTimeAndShowBalloons(object sender, EventArgs e)
		{
			if (DateTime.Now.Second == 25 || DateTime.Now.Second == 55) {
				balloonTimer.Stop();
				MakeBallons();
			}
		}

		/// <summary>
		/// Generate a random number of balloons and then a random colour and display them on the screen ready to be moved.
		/// </summary>
		private void MakeBallons()
		{
			int numberOfBallons = Rnd.Next(5, MAX_BALLOONS);
			BalloonImages.Clear();
			BalloonSpeeds.Clear();
			Canvas_Balloons.Children.Clear();

			// loop through the randomly generated number of balloons to create.
			for (int i = 0; i < numberOfBallons; i++) {
				int balloonVariation = Rnd.Next(1, 5);
				Image balloon = new Image() {
					Source = new BitmapImage(new Uri($"Images/balloon-{balloonVariation}.png", UriKind.Relative)),
					Width = 140,
					Stretch = Stretch.UniformToFill
				};
				// Place the balloon on the screen 
				Canvas_Balloons.Children.Add(balloon);
				Canvas.SetTop(balloon, this.Height + 210);
				Canvas.SetLeft(balloon, Rnd.Next(75, (int)(WindowWidth - 75)));

				// Add the balloons to the list so that they can be moved at random speeds
				BalloonImages.Add(balloon);
				BalloonSpeeds.Add(Rnd.Next(10, 20));
			}

			// Start timer to move the balloons
			movementTimer = new DispatcherTimer();
			movementTimer.Tick += MoveBalloons;
			movementTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
			movementTimer.Start();
		}

		/// <summary>
		/// Move all the balloons on the screen and make it look like they are rising to the top of the monitor.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MoveBalloons(object sender, EventArgs e)
		{
			stopTimer = true;

			// Loop over each balloon on the page and move it
			for (int i = 0; i < BalloonImages.Count; i++) {
				Image balloon = BalloonImages[i];
				Canvas.SetTop(balloon, Canvas.GetTop(balloon) - BalloonSpeeds[i]);
				if (Canvas.GetTop(balloon) > -210) {
					stopTimer = false;
				}
			}

			// Stop the timer if all balloons are not visible
			if (stopTimer) {
				movementTimer.Stop();
				balloonTimer.Start();
			}
		}


		#region Window styles

		[Flags]
		private enum ExtendedWindowStyles
		{
			WS_EX_TOOLWINDOW = 0x00000080
		}

		private enum GetWindowLongFields
		{
			GWL_EXSTYLE = (-20)
		}

		/// <summary>
		/// The method to remove the application from the taskbar and alt-tab menu.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="nIndex"></param>
		/// <param name="dwNewLong"></param>
		/// <returns></returns>
		public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			int error;
			IntPtr result;

			// Win32 SetWindowLong doesn't clear error on success
			SetLastError(0);

			if (IntPtr.Size == 4) {
				// use SetWindowLong
				int tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
				error = Marshal.GetLastWin32Error();
				result = new IntPtr(tempResult);
			} else {
				// use SetWindowLongPtr
				result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
				error = Marshal.GetLastWin32Error();
			}

			return (result != null && result == IntPtr.Zero) && (error != 0) 
				? throw new System.ComponentModel.Win32Exception(error) 
				: result;
		}

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
		private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
		private static extern int IntSetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("kernel32.dll", EntryPoint = "SetLastError")]
		public static extern void SetLastError(int dwErrorCode);

		private static int IntPtrToInt32(IntPtr intPtr) => unchecked((int)intPtr.ToInt64());

		#endregion


	}
}
