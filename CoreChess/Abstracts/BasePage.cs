using Avalonia.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Input;
using CoreChess.Views;

namespace CoreChess.Abstracts
{
    public class BasePage : UserControl, IDisposable
    {
        public enum NavigationDirection
        {
            Forward,
            Backward
        }

        public abstract class PageState
        {
            public PageState(BasePage page) {
                PageId = page.Id;
            }

            public string PageId {get; private set; }
        } // PageState

        #region events
        public delegate void NavigatingHandler(object sender);
        public event NavigatingHandler Navigating;
        #endregion

        public BasePage()
        {
            Id = Guid.NewGuid().ToString("N");

            NavigateBackWithKeyboard = true;
            NavigateBackOnWindowClose = true;
        }

        protected MainWindow MainWindow
        {
            get {
                return App.MainWindow;
            }
        }

        public string Id { get; private set; }
        public bool NavigateBackWithKeyboard { get; set; }
        public bool NavigateBackOnWindowClose { get; set;}
        public string PageTitle { get; set; }

        public bool CanNavigateBack
        {
            get { return MainWindow!.CanNavigateBack; }
        }

        /// <summary>
        /// Called before navigating to a new page
        /// </summary>
        /// <param name="direction">The navigation direction</param>
        /// <returns>True to allow the navigation, false to deny it</returns>
        public virtual Task<bool> OnNavigatingFrom(NavigationDirection direction)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called after the navigation to this page
        /// </summary>
        /// <param name="direction"></param>
        public virtual void OnNavigatedTo(NavigationDirection direction)
        {
            if (!string.IsNullOrEmpty(PageTitle)) {
                if (!string.IsNullOrEmpty(MainWindow!.WindowTitle))
                    MainWindow.Title = $"{MainWindow.WindowTitle} - {PageTitle}";
                else
                    MainWindow.Title = PageTitle;
            } else {
                MainWindow!.Title = MainWindow.WindowTitle;
            }
        }

        public virtual void Dispose()
        {

        }

        /// <summary>
        /// Key down event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        public virtual void HandleWindowStateChanged(WindowState state)
        {

        }

        public async Task<bool> NavigateTo(BasePage page)
        {
            return await MainWindow!.NavigateTo(page);
        }

        public async Task<bool> NavigateBack()
        {
            var res =await MainWindow!.NavigateBack();
            if (res)
                Navigating?.Invoke(this);
            return res;
        }

        public void SaveState(PageState state) {
            MainWindow!.SavePageState(state);
        } // SaveState

        public PageState LoadState<T>() where T: PageState
        {
            return MainWindow!.LoadPageState<T>(this);
        } // LoadState
    }
}
