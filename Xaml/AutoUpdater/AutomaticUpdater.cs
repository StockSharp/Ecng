using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ecng.Xaml.AutoUpdater
{
    /// <summary>Represents the AutomaticUpdater control.</summary>
    public class AutomaticUpdater : Control
    {
        enum State { Info, UpdateNotify, Working, Tick, Error }

        const Visibility _hiddenVisibility = Visibility.Collapsed;

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(AutomaticUpdater), new PropertyMetadata(false));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(AutomaticUpdater), new PropertyMetadata(default(string)));

        public bool IsExpanded {get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value);}
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        readonly AutomaticUpdaterBackend auBackend = new AutomaticUpdaterBackend();

        Window ownerForm;
        bool _cancelStartupCheck;
        FailArgs failArgs;

        readonly ContextMenu contextMenu = new ContextMenu();
        MenuType CurrMenuType = MenuType.Nothing;

        // changes
        bool ShowButtonUpdateNow;

        string currentActionText;

        // menu items
        MenuItem menuItem;

        static AutomaticUpdater() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutomaticUpdater), new FrameworkPropertyMetadata(typeof(AutomaticUpdater)));
        }

        #region Events

        /// <summary>Event is raised before the update checking begins.</summary>
        [Description("Event is raised before the update checking begins."),
        Category("Updater")]
        public event BeforeHandler BeforeChecking;

        /// <summary>Event is raised before the downloading of the update begins.</summary>
        [Description("Event is raised before the downloading of the update begins."),
        Category("Updater")]
        public event BeforeHandler BeforeDownloading;

        /// <summary>Event is raised before the installation of the update begins.</summary>
        [Description("Event is raised before the installation of the update begins."),
        Category("Updater")]
        public event BeforeHandler BeforeInstalling;

        /// <summary>Event is raised when checking or updating is cancelled.</summary>
        [Description("Event is raised when checking or updating is cancelled."),
        Category("Updater")]
        public event EventHandler Cancelled;

        /// <summary>Event is raised when the checking for updates fails.</summary>
        [Description("Event is raised when the checking for updates fails."),
        Category("Updater")]
        public event FailHandler CheckingFailed;

        /// <summary>Event is raised after you or your user invoked InstallNow(). You should close your app as quickly as possible (because wyUpdate will be waiting).</summary>
        [Description("Event is raised after you or your user invoked InstallNow(). You should close your app as quickly as possible (because wyUpdate will be waiting)."),
        Category("Updater")]
        public event EventHandler CloseAppNow;

        /// <summary>Event is raised when an update can't be installed and the closing is aborted.</summary>
        [Description("Event is raised when an update can't be installed and the closing is aborted."),
        Category("Updater")]
        public event EventHandler ClosingAborted;

        /// <summary>Event is raised when the update fails to download or extract.</summary>
        [Description("Event is raised when the update fails to download or extract."),
        Category("Updater")]
        public event FailHandler DownloadingOrExtractingFailed;

        /// <summary>Event is raised when the current update step progress changes.</summary>
        [Description("Event is raised when the current update step progress changes."),
        Category("Updater")]
        public event UpdateProgressChanged ProgressChanged;

        /// <summary>Event is raised when the update is ready to be installed.</summary>
        [Description("Event is raised when the update is ready to be installed."),
        Category("Updater")]
        public event EventHandler ReadyToBeInstalled;

        /// <summary>Event is raised when a new update is found.</summary>
        [Description("Event is raised when a new update is found."),
        Category("Updater")]
        public event EventHandler UpdateAvailable;

        /// <summary>Event is raised when an update fails to install.</summary>
        [Description("Event is raised when an update fails to install."),
        Category("Updater")]
        public event FailHandler UpdateFailed;

        /// <summary>Event is raised when an update installs successfully.</summary>
        [Description("Event is raised when an update installs successfully."),
        Category("Updater")]
        public event SuccessHandler UpdateSuccessful;

        /// <summary>Event is raised when the latest version is already installed.</summary>
        [Description("Event is raised when the latest version is already installed."),
        Category("Updater")]
        public event SuccessHandler UpToDate;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets the arguments to pass to your app when it's being restarted after an update.
        /// </summary>
        [Description("The arguments to pass to your app when it's being restarted after an update."),
        DefaultValue(null),
        Category("Updater")]
        public string Arguments
        {
            get { return auBackend.Arguments; }
            set { auBackend.Arguments = value; }
        }

        /// <summary>
        /// Gets the changes for the new update.
        /// </summary>
        [Browsable(false)]
        public string Changes { get { return auBackend.Changes; } }

        /// <summary>
        /// Gets if this AutomaticUpdater has hidden this form and preparing to install an update.
        /// </summary>
        [Browsable(false)]
        public bool ClosingForInstall { get { return auBackend.ClosingForInstall; } }

        /// <summary>
        /// Gets or sets the number of days to wait before automatically re-checking for updates.
        /// </summary>
        [Description("The number of days to wait before automatically re-checking for updates."),
        DefaultValue(12),
        Category("Updater")]
        public int DaysBetweenChecks { get; set; } = 12;

        /// <summary>
        /// Gets the GUID (Globally Unique ID) of the automatic updater. It is recommended you set this value (especially if there is more than one exe for your product).
        /// </summary>
        /// <exception cref="System.Exception">Thrown when trying to set the GUID at runtime.</exception>
        [Description("The GUID (Globally Unique ID) of the automatic updater. It is recommended you set this value (especially if there is more than one exe for your product)."),
        Category("Updater"),
        DefaultValue(null),
        EditorBrowsable(EditorBrowsableState.Never)]
        public string GUID
        {
            get { return auBackend.GUID; }
            set { auBackend.GUID = value; }
        }

        bool ShouldSerializeGUID()
        {
            return !string.IsNullOrEmpty(auBackend.GUID);
        }

        /// <summary>
        /// Gets or sets whether the AutomaticUpdater control should stay hidden even when the user should be notified. (Not recommended).
        /// </summary>
        [Description("Keeps the AutomaticUpdater control hidden even when the user should be notified. (Not recommended)"),
        DefaultValue(false),
        Category("Updater")]
        public bool KeepHidden { get; set; }

        /// <summary>
        /// Gets the date the updates were last checked for.
        /// </summary>
        [Browsable(false)]
        public DateTime LastCheckDate
        {
            get { return auBackend.LastCheckDate; }
        }

        /// <summary>
        /// Gets and sets the MenuItem that will be used to check for updates.
        /// </summary>
        [Description("The MenuItem that will be used to check for updates."),
        Category("Updater"),
        DefaultValue(null)]
        public MenuItem MenuItem
        {
            get
            {
                return menuItem;
            }
            set
            {
                if (menuItem != null)
                    menuItem.Click -= menuItem_Click;

                menuItem = value;

                if (menuItem != null)
                    menuItem.Click += menuItem_Click;
            }
        }

        AUTranslation translation = new AUTranslation();

        /// <summary>
        /// The translation for the english strings in the AutomaticUpdater control.
        /// </summary>
        /// <exception cref="ArgumentNullException">The translation cannot be null.</exception>
        [Browsable(false)]
        public AUTranslation Translation
        {
            get { return translation; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                translation = value;
            }
        }

        bool ShouldSerializeTranslation() { return false; }

        /// <summary>
        /// Gets the update step the AutomaticUpdater is currently on.
        /// </summary>
        [Browsable(false)]
        public UpdateStepOn UpdateStepOn { get { return auBackend.UpdateStepOn; } }


        /// <summary>
        /// Gets or sets how much this AutomaticUpdater control should do without user interaction.
        /// </summary>
        [Description("How much this AutomaticUpdater control should do without user interaction."),
        DefaultValue(UpdateType.Automatic),
        Category("Updater")]
        public UpdateType UpdateType
        {
            get { return auBackend.UpdateType; }
            set { auBackend.UpdateType = value; }
        }

        /// <summary>
        /// Gets the version of the new update.
        /// </summary>
        [Browsable(false)]
        public string Version { get { return auBackend.Version; } }

        /// <summary>
        /// Gets or sets the seconds to wait after the form is loaded before checking for updates.
        /// </summary>
        [Description("Seconds to wait after the form is loaded before checking for updates."),
        DefaultValue(10),
        Category("Updater")]
        public int WaitBeforeCheckSecs { get; set; } = 10;

        /// <summary>
        /// Gets or sets the arguments to pass to wyUpdate when it is started to check for updates.
        /// </summary>
        [Description("Arguments to pass to wyUpdate when it is started to check for updates."),
        Category("Updater")]
        public string wyUpdateCommandline
        {
            get { return auBackend.wyUpdateCommandline; }
            set { auBackend.wyUpdateCommandline = value; }
        }

        /// <summary>
        /// Gets or sets the relative path to the wyUpdate (e.g. wyUpdate.exe  or  SubDir\\wyUpdate.exe)
        /// </summary>
        [Description("The relative path to the wyUpdate (e.g. wyUpdate.exe  or  SubDir\\wyUpdate.exe)"),
        DefaultValue("wyUpdate.exe"),
        Category("Updater")]
        public string wyUpdateLocation
        {
            get { return auBackend.wyUpdateLocation; }
            set { auBackend.wyUpdateLocation = value; }
        }

        #endregion

        public AutomaticUpdater()
        {
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.PlacementTarget = this;

            // use all events from the AutomaticUpdater backend
            auBackend.BeforeChecking += auBackend_BeforeChecking;
            auBackend.BeforeDownloading += auBackend_BeforeDownloading;
            auBackend.BeforeExtracting += auBackend_BeforeExtracting;
            auBackend.BeforeInstalling += auBackend_BeforeInstalling;
            auBackend.Cancelled += auBackend_Cancelled;
            auBackend.ReadyToBeInstalled += auBackend_ReadyToBeInstalled;
            auBackend.UpdateAvailable += auBackend_UpdateAvailable;
            auBackend.UpdateSuccessful += auBackend_UpdateSuccessful;
            auBackend.UpToDate += auBackend_UpToDate;

            auBackend.ProgressChanged += auBackend_ProgressChanged;

            auBackend.CheckingFailed += auBackend_CheckingFailed;
            auBackend.DownloadingFailed += auBackend_DownloadingFailed;
            auBackend.ExtractingFailed += auBackend_ExtractingFailed;
            auBackend.UpdateFailed += auBackend_UpdateFailed;

            auBackend.ClosingAborted += auBackend_ClosingAborted;
            auBackend.UpdateStepMismatch += auBackend_UpdateStepMismatch;

            auBackend.CloseAppNow += auBackend_CloseAppNow;

            var initialized = false;

            Initialized += (sender, args) => {
                if(initialized)
                    return;

                initialized = true;
                Init();
            };
        }

        void SetState(State state) {
            _icon.Source = _stateIcons[state];
        }

        void auBackend_CloseAppNow(object sender, EventArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new EventHandler(auBackend_CloseAppNow), null, e);
                return;
            }

            // close this application so it can be updated
            if (CloseAppNow != null)
                CloseAppNow(this, EventArgs.Empty);
            else
                Application.Current.Shutdown();
        }

        void auBackend_UpToDate(object sender, SuccessArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SuccessHandler(auBackend_UpToDate), null, e);
                return;
            }

            Text = translation.AlreadyUpToDate;

            if (Visibility == Visibility.Visible)
                UpdateStepSuccessful(MenuType.AlreadyUpToDate);

            SetMenuText(translation.CheckForUpdatesMenu);

            if (UpToDate != null)
                UpToDate(this, e);
        }

        void auBackend_UpdateSuccessful(object sender, SuccessArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SuccessHandler(auBackend_UpdateSuccessful), null, e);
                return;
            }

            // show the control
            Visibility = KeepHidden ? _hiddenVisibility : Visibility.Visible;

            Text = translation.SuccessfullyUpdated.Replace("%version%", Version);
            UpdateStepSuccessful(MenuType.UpdateSuccessful);

            if (UpdateSuccessful != null)
                UpdateSuccessful(this, e);
        }

        void auBackend_UpdateFailed(object sender, FailArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FailHandler(auBackend_UpdateFailed), null, e);
                return;
            }

            // show the control
            Visibility = KeepHidden ? _hiddenVisibility : Visibility.Visible;

            failArgs = e;

            // show failed Text & icon
            Text = translation.UpdateFailed;
            CreateMenu(MenuType.Error);
            SetState(State.Error);

            if (UpdateFailed != null)
                UpdateFailed(this, e);
        }

        void auBackend_UpdateAvailable(object sender, EventArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new EventHandler(auBackend_UpdateAvailable), null, e);
                return;
            }

            CreateMenu(MenuType.DownloadAndChanges);

            SetUpdateStepOn(UpdateStepOn.UpdateAvailable);

            if (!KeepHidden)
                Visibility = Visibility.Visible;

            SetCurrentValue(IsExpandedProperty, true);
            SetState(State.UpdateNotify);

            SetMenuText(translation.DownloadUpdateMenu);

            if (UpdateAvailable != null)
                UpdateAvailable(this, e);
        }

        void auBackend_ReadyToBeInstalled(object sender, EventArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new EventHandler(auBackend_ReadyToBeInstalled), null, e );
                return;
            }

            CreateMenu(MenuType.InstallAndChanges);

            // UpdateDownloaded or UpdateReadyToInstall
            SetUpdateStepOn(auBackend.UpdateStepOn);

            if (!KeepHidden)
                Visibility = Visibility.Visible;

            SetCurrentValue(IsExpandedProperty, true);
            SetState(State.Info);

            SetMenuText(translation.InstallUpdateMenu);


            if (ReadyToBeInstalled != null)
                ReadyToBeInstalled(this, e);
        }

        void auBackend_Cancelled(object sender, EventArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new EventHandler(auBackend_Cancelled), null, e);
                return;
            }

            Visibility = _hiddenVisibility;

            SetMenuText(translation.CheckForUpdatesMenu);

            if (Cancelled != null)
                Cancelled(this, e);
        }

        void auBackend_BeforeChecking(object sender, BeforeArgs e)
        {
            // disable any scheduled checking
            _cancelStartupCheck = true;

            SetMenuText(translation.CancelCheckingMenu);

            if (BeforeChecking != null)
                BeforeChecking(this, e);

            if (e.Cancel)
            {
                // close wyUpdate
                auBackend.Cancel();
                return;
            }

            // show the working animation
            SetUpdateStepOn(UpdateStepOn.Checking);
            UpdateProcessing(false);

            // setup the context menu
            CreateMenu(MenuType.CheckingMenu);
        }

        void auBackend_BeforeDownloading(object sender, BeforeArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new BeforeHandler(auBackend_BeforeDownloading), null, e);
                return;
            }

            if (BeforeDownloading != null)
                BeforeDownloading(this, e);

            if (e.Cancel)
                return;

            SetMenuText(translation.CancelUpdatingMenu);

            // if the control is hidden show it now (so the user can cancel the downloading if they want)
            // show the 'working' animation
            SetUpdateStepOn(UpdateStepOn.DownloadingUpdate);
            UpdateProcessing(true);

            CreateMenu(MenuType.CancelDownloading);
        }

        void auBackend_BeforeExtracting(object sender, BeforeArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new BeforeHandler(auBackend_BeforeExtracting), null, e);
                return;
            }

            SetUpdateStepOn(UpdateStepOn.ExtractingUpdate);

            CreateMenu(MenuType.CancelExtracting);
        }

        void auBackend_BeforeInstalling(object sender, BeforeArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new BeforeHandler(auBackend_BeforeInstalling), null, e);
                return;
            }

            if (BeforeInstalling != null)
                BeforeInstalling(this, e);
        }

        void auBackend_CheckingFailed(object sender, FailArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FailHandler(auBackend_CheckingFailed), null, e);
                return;
            }

            UpdateStepFailed(e);

            Text = translation.FailedToCheck;

            if (CheckingFailed != null)
                CheckingFailed(this, e);
        }

        void auBackend_DownloadingFailed(object sender, FailArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FailHandler(auBackend_DownloadingFailed), null, e);
                return;
            }

            UpdateStepFailed(e);

            Text = translation.FailedToDownload;

            if (DownloadingOrExtractingFailed != null)
                DownloadingOrExtractingFailed(this, e);
        }

        void auBackend_ExtractingFailed(object sender, FailArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FailHandler(auBackend_ExtractingFailed), null, e);
                return;
            }

            UpdateStepFailed(e);

            Text = translation.FailedToExtract;

            if (DownloadingOrExtractingFailed != null)
                DownloadingOrExtractingFailed(this, e);
        }

        void UpdateStepFailed(FailArgs e)
        {
            if (e.wyUpdatePrematureExit)
            {
                e.ErrorTitle = translation.PrematureExitTitle;

                // use the general "premature exit" message only when there's no other message present
                if (e.ErrorMessage == null || e.ErrorMessage == AUTranslation.C_PrematureExitMessage)
                    e.ErrorMessage = translation.PrematureExitMessage;
            }

            failArgs = e;

            //only show the error if this is visible
            if (Visibility == Visibility.Visible)
            {
                CreateMenu(MenuType.Error);
                SetState(State.Error);
            }

            SetMenuText(translation.CheckForUpdatesMenu);
        }

        void auBackend_ProgressChanged(object sender, int progress)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateProgressChanged(auBackend_ProgressChanged), null, progress);
                return;
            }

            // update progress status (only for greater than 0%)
            if (progress > 0)
                Text = currentActionText + ", " + progress + "%";

            // call the progress changed event
            if (ProgressChanged != null)
                ProgressChanged(this, progress);
        }

        void auBackend_ClosingAborted(object sender, EventArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new EventHandler(auBackend_ClosingAborted), null, e);
                return;
            }

            // we need to show the form (it was hidden in ISupport() )
            if (auBackend.ClosingForInstall)
            {
                ownerForm.ShowInTaskbar = true;
                ownerForm.WindowState = WindowState.Normal;
            }

            if (ClosingAborted != null)
                ClosingAborted(this, EventArgs.Empty);
        }

        void auBackend_UpdateStepMismatch(object sender, EventArgs e)
        {
            // call this function from ownerForm's thread context
            if (sender != null)
            {
                ownerForm.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new EventHandler(auBackend_UpdateStepMismatch), null, e);
                return;
            }

            SetUpdateStepOn(auBackend.UpdateStepOn);

            // set the working progress animation
            if (auBackend.UpdateStepOn == UpdateStepOn.Checking
                || auBackend.UpdateStepOn == UpdateStepOn.DownloadingUpdate
                || auBackend.UpdateStepOn == UpdateStepOn.ExtractingUpdate)
            {
                UpdateProcessing(false);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (contextMenu.IsOpen)
                contextMenu.IsOpen = false;
            else
                contextMenu.IsOpen = true;

            base.OnMouseDown(e);
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            switch (UpdateStepOn)
            {
                case UpdateStepOn.Checking:
                case UpdateStepOn.DownloadingUpdate:
                case UpdateStepOn.ExtractingUpdate:

                    Cancel();
                    break;

                case UpdateStepOn.UpdateReadyToInstall:
                case UpdateStepOn.UpdateAvailable:
                case UpdateStepOn.UpdateDownloaded:

                    InstallNow();
                    break;

                default:

                    ForceCheckForUpdate(false, true);
                    break;
            }
        }

        void SetMenuText(string text)
        {
            if (menuItem != null)
                menuItem.Header = text;
        }


        void InstallNow_Click(object sender, EventArgs e)
        {
            InstallNow();
        }

        /// <summary>
        /// Proceed with the download and installation of pending updates.
        /// </summary>
        public void InstallNow()
        {
            auBackend.InstallNow();
        }

        void CancelUpdate_Click(object sender, EventArgs e)
        {
            Cancel();
        }

        void Hide_Click(object sender, EventArgs e)
        {
            Visibility = _hiddenVisibility;
        }

        /// <summary>
        /// Cancel the checking, downloading, or extracting currently in progress.
        /// </summary>
        public void Cancel()
        {
            auBackend.Cancel();
        }

        void ViewChanges_Click(object sender, EventArgs e)
        {
            var dlg = new ChangesDialog(auBackend.Version, auBackend.RawChanges, auBackend.AreChangesRTF, ShowButtonUpdateNow, translation) {
                Owner = ownerForm
            };

            dlg.ShowDialog();

            if(dlg.UpdateNow)
                InstallNow();
        }

        void ViewError_Click(object sender, EventArgs e)
        {
            var dlg = new ErrorDialog(failArgs, translation);
            dlg.ShowDialog();

            if (dlg.TryAgainLater)
                TryAgainLater_Click(this, EventArgs.Empty);
        }

        void TryAgainLater_Click(object sender, EventArgs e)
        {
            // we'll check on next start of this app,
            // just hide for now.
            Visibility = _hiddenVisibility;
        }

        void TryAgainNow_Click(object sender, EventArgs e)
        {
            // check for updates (if we're actually further along, wyUpdate will set us straight)
            ForceCheckForUpdate(true);
        }


        void CreateMenu(MenuType NewMenuType)
        {
            // if the context menu is visible
            if (contextMenu.IsOpen)
            {
                // hide the context menu
                contextMenu.IsOpen = false;
            }


            // destroy previous menu type (by removing events associated with it)
            // unregister the events to existing menu items
            switch (CurrMenuType)
            {
                case MenuType.CancelDownloading:
                case MenuType.CancelExtracting:
                case MenuType.CheckingMenu:
                    ((MenuItem)contextMenu.Items[0]).Click -= CancelUpdate_Click;
                    break;

                case MenuType.InstallAndChanges:
                case MenuType.DownloadAndChanges:
                    ((MenuItem)contextMenu.Items[0]).Click -= InstallNow_Click;
                    ((MenuItem)contextMenu.Items[1]).Click -= ViewChanges_Click;
                    break;

                case MenuType.Error:
                    ((MenuItem)contextMenu.Items[0]).Click -= TryAgainLater_Click;
                    ((MenuItem)contextMenu.Items[1]).Click -= TryAgainNow_Click;
                    ((MenuItem)contextMenu.Items[3]).Click -= ViewError_Click;
                    break;

                case MenuType.UpdateSuccessful:
                    ((MenuItem)contextMenu.Items[0]).Click -= Hide_Click;
                    ((MenuItem)contextMenu.Items[1]).Click -= ViewChanges_Click;
                    break;

                case MenuType.AlreadyUpToDate:
                    ((MenuItem)contextMenu.Items[0]).Click -= Hide_Click;
                    break;
            }

            contextMenu.Items.Clear();

            // create new menu type & add new events

            switch (NewMenuType)
            {
                case MenuType.Nothing:
                    break;
                case MenuType.CheckingMenu:
                    MenuItem mi = new MenuItem { Header = translation.StopChecking };
                    mi.Click += CancelUpdate_Click;
                    contextMenu.Items.Add(mi);
                    break;
                case MenuType.CancelDownloading:
                    mi = new MenuItem { Header = translation.StopDownloading };
                    mi.Click += CancelUpdate_Click;
                    contextMenu.Items.Add(mi);
                    break;
                case MenuType.CancelExtracting:
                    mi = new MenuItem { Header = translation.StopExtracting };
                    mi.Click += CancelUpdate_Click;
                    contextMenu.Items.Add(mi);
                    break;
                case MenuType.InstallAndChanges:
                case MenuType.DownloadAndChanges:

                    mi = new MenuItem { Header = NewMenuType == MenuType.InstallAndChanges ? translation.InstallUpdateMenu : translation.DownloadUpdateMenu };
                    mi.Click += InstallNow_Click;
                    mi.FontWeight = FontWeights.Bold;
                    contextMenu.Items.Add(mi);

                    mi = new MenuItem { Header = translation.ViewChangesMenu.Replace("%version%", Version) };
                    mi.Click += ViewChanges_Click;
                    contextMenu.Items.Add(mi);

                    ShowButtonUpdateNow = true;

                    break;
                case MenuType.Error:

                    mi = new MenuItem { Header = translation.TryAgainLater };
                    mi.Click += TryAgainLater_Click;
                    mi.FontWeight = FontWeights.Bold;
                    contextMenu.Items.Add(mi);

                    mi = new MenuItem { Header = translation.TryAgainNow };
                    mi.Click += TryAgainNow_Click;
                    contextMenu.Items.Add(mi);

                    contextMenu.Items.Add(new Separator());

                    mi = new MenuItem { Header = translation.ViewError };
                    mi.Click += ViewError_Click;
                    contextMenu.Items.Add(mi);

                    break;
                case MenuType.UpdateSuccessful:

                    mi = new MenuItem { Header = translation.HideMenu };
                    mi.Click += Hide_Click;
                    contextMenu.Items.Add(mi);

                    mi = new MenuItem { Header = translation.ViewChangesMenu.Replace("%version%", Version) };
                    mi.Click += ViewChanges_Click;
                    contextMenu.Items.Add(mi);

                    ShowButtonUpdateNow = false;

                    break;

                case MenuType.AlreadyUpToDate:
                    mi = new MenuItem { Header = translation.HideMenu };
                    mi.Click += Hide_Click;
                    contextMenu.Items.Add(mi);
                    break;
            }

            CurrMenuType = NewMenuType;
        }

        /// <summary>
        /// Check for updates forcefully -- returns true if the updating has begun. Use the "CheckingFailed", "UpdateAvailable", or "UpToDate" events for the result.
        /// </summary>
        /// <param name="recheck">Recheck with the servers regardless of cached updates, etc.</param>
        /// <returns>Returns true if checking has begun, false otherwise.</returns>
        public bool ForceCheckForUpdate(bool recheck)
        {
            return ForceCheckForUpdate(recheck, false);
        }

        bool ForceCheckForUpdate(bool recheck, bool fromUI)
        {
            // if not already checking for updates then begin checking.
            if (UpdateStepOn == UpdateStepOn.Nothing || (recheck && UpdateStepOn == UpdateStepOn.UpdateAvailable))
            {
                if (recheck || fromUI)
                    Visibility = KeepHidden ? _hiddenVisibility : Visibility.Visible;

                return auBackend.ForceCheckForUpdate(recheck);
            }

            return false;
        }

        /// <summary>
        /// Check for updates forcefully -- returns true if the updating has begun. Use the "CheckingFailed", "UpdateAvailable", or "UpToDate" events for the result.
        /// </summary>
        /// <returns>Returns true if checking has begun, false otherwise.</returns>
        public bool ForceCheckForUpdate()
        {
            return ForceCheckForUpdate(false, false);
        }

        void UpdateStepSuccessful(MenuType menuType)
        {
            // create the "hide" menu
            CreateMenu(menuType);

            SetCurrentValue(IsExpandedProperty, true);
            SetState(State.Tick);
        }

        void UpdateProcessing(bool forceShow)
        {
            if (forceShow && !KeepHidden)
                Visibility = Visibility.Visible;

            SetCurrentValue(IsExpandedProperty, true);
            SetState(State.Working);
        }

        void SetUpdateStepOn(UpdateStepOn uso)
        {
            switch (uso)
            {
                case UpdateStepOn.Checking:
                    Text = currentActionText = translation.Checking;
                    break;

                case UpdateStepOn.DownloadingUpdate:
                    Text = currentActionText = translation.Downloading;
                    break;

                case UpdateStepOn.ExtractingUpdate:
                    Text = currentActionText = translation.Extracting;
                    break;

                case UpdateStepOn.UpdateAvailable:
                    Text = translation.UpdateAvailable;
                    break;

                case UpdateStepOn.UpdateDownloaded:
                    Text = translation.UpdateAvailable;
                    break;

                case UpdateStepOn.UpdateReadyToInstall:
                    Text = translation.InstallOnNextStart;
                    break;
            }
        }

        void Init()
        {
            if (DesignMode)
                return;
        
            ownerForm = Window.GetWindow(this);
        
            if (ownerForm == null)
                throw new Exception("Could not find the AutomaticUpdater's owner Window. Make sure you're adding the AutomaticUpdater to a Window and not a View, User control, etc.");
        
            ownerForm.Loaded += ownerForm_Loaded;
        
            auBackend.Initialize();
        
            // see if update is pending, if so force install
            if (auBackend.ClosingForInstall)
            {
                // hide self if there's an update pending
                ownerForm.ShowInTaskbar = false;
                ownerForm.WindowState = WindowState.Minimized;
            }
        }

        async void ownerForm_Loaded(object sender, RoutedEventArgs e)
        {
            SetMenuText(translation.CheckForUpdatesMenu);

            // if we want to kill ouself, then don't bother checking for updates
            if (auBackend.ClosingForInstall)
                return;

            auBackend.AppLoaded();

            Visibility = _hiddenVisibility;

            if (UpdateStepOn != UpdateStepOn.Nothing)
            {
                // show the updater control
                if (!KeepHidden)
                    Visibility = Visibility.Visible;
            }
            else if (auBackend.UpdateType != UpdateType.DoNothing) // UpdateStepOn == UpdateStepOn.Nothing
            {
                // see if enough days have elapsed since last check.
                TimeSpan span = DateTime.Now.Subtract(auBackend.LastCheckDate);

                if (span.Days >= DaysBetweenChecks)
                {
                    await Task.Delay(TimeSpan.FromSeconds(WaitBeforeCheckSecs));

                    if(!_cancelStartupCheck) {
                        Visibility = _hiddenVisibility;
                        auBackend.ForceCheckForUpdate(false);
                    }
                }
            }
        }

        bool? isDesign;

        bool DesignMode
        {
            get
            {
                if (!isDesign.HasValue)
                    isDesign = DesignerProperties.GetIsInDesignMode(new DependencyObject());

                return isDesign.Value;
            }
        }
       
        Image _icon;

        readonly Dictionary<State, ImageSource> _stateIcons = new Dictionary<State, ImageSource>();

        public override void OnApplyTemplate()
        {
            var asmName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            
            ImageSource CreateIcon(string name) => new BitmapImage(new Uri($"pack://application:,,,/{asmName};component/Resources/{name}.png"));

            base.OnApplyTemplate();
        
            _icon = GetTemplateChild("PART_Icon") as Image;

            _stateIcons[State.Info] = CreateIcon("info");
            _stateIcons[State.UpdateNotify] = CreateIcon("update-notify");
            _stateIcons[State.Tick] = CreateIcon("tick");
            _stateIcons[State.Error] = CreateIcon("cross");
            _stateIcons[State.Working] = CreateIcon("wait");
        }
    }

    public class MultiplyConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            var width       = (double)values[0];
            var multiplier  = values[1] as double? ?? 0d;

            return width * multiplier;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}