using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace EventMapHpViewer
{
	public class ToolViewModel
	{
		/// <summary>
		/// ポップアップウィンドウのインスタンスを保持します（多重起動防止用）。
		/// </summary>
		private ToolViewWindow _popupWindow;


		#region IsTopMost 変更通知プロパティ

		private bool _IsTopMost = true;

		/// <summary>
		/// ポップアップウィンドウを最前面に固定するかどうかを切り替えます。
		/// </summary>
		public bool IsTopMost
		{
			get { return this._IsTopMost; }
			set
			{
				if (this._IsTopMost != value)
				{
					this._IsTopMost = value;
					this.OnPropertyChanged();
				}
			}
		}

		#endregion

		#region IsPopupMode 変更通知プロパティ

		private bool _IsPopupMode;

		/// <summary>
		/// ポップアップウィンドウ内で表示中かどうかを示します。
		/// true の場合、「別ウィンドウで表示」ボタンを非表示にします。
		/// </summary>
		public bool IsPopupMode
		{
			get { return this._IsPopupMode; }
			set
			{
				if (this._IsPopupMode != value)
				{
					this._IsPopupMode = value;
					this.OnPropertyChanged();
				}
			}
		}

		#endregion

		/// <summary>
		/// ポップアップウィンドウを開きます。
		/// 既に開いている場合はアクティブにします。
		/// </summary>
		public void OpenPopupWindow()
		{
			try
			{
				if (this._popupWindow != null && this._popupWindow.IsLoaded)
				{
					this._popupWindow.Activate();
					return;
				}

				// ポップアップ用に ViewModel を複製せず共有するため、フラグで制御
				this.IsPopupMode = true;

				this._popupWindow = new ToolViewWindow
                {
					DataContext = this,
				};

				this._popupWindow.Closed += (s, e) =>
				{
					this.IsPopupMode = false;
					this._popupWindow = null;
				};

				this._popupWindow.Show();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[ToolViewWindow] ポップアップウィンドウの表示に失敗: {ex.Message}");
				this.IsPopupMode = false;
				this._popupWindow = null;
			}
		}

		/// <summary>
		/// ポップアップウィンドウを安全に閉じます。
		/// </summary>
		public void ClosePopupWindow()
		{
			try
			{
				if (this._popupWindow != null && this._popupWindow.IsLoaded)
				{
					this._popupWindow.Close();
				}
			}
			catch
			{
			}
			finally
			{
				this.IsPopupMode = false;
				this._popupWindow = null;
			}
		}

		/// <summary>
		/// null または 空文字列 なら Collapsed、そうでなければ Visible を返すコンバーターです。
		/// </summary>
		public class NullToCollapsedConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var s = value as string;
				if (string.IsNullOrEmpty(s)) return Visibility.Collapsed;
				return Visibility.Visible;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// bool が true なら Visible、false なら Collapsed を返すコンバーターです。
		/// </summary>
		public class BoolToVisibilityConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (value is bool b && b)
				{
					return Visibility.Visible;
				}
				return Visibility.Collapsed;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotSupportedException();
			}
		}
	}
}
