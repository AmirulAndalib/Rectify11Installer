using KPreisser.UI;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Rectify11Installer.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rectify11Installer.Pages
{
	public partial class ProgressPage : WizardPage
	{
		#region Variables
		private FrmWizard frmwiz;
		private Timer timer2;
		private int duration = 30;
		private int CurrentTextIndex = -1;
		private static readonly InstallerTexts[] Rectify11InstallerTexts =
		{
			new("Did you know that...", "Rectify11 has better Win32 DPI support because we scale controls correctly.", Properties.Resources.dpi),
			new("Rectify11 has a better Theme", "We have tried our best to replicate WinUI controls in our themes, and the dark theme is just amazing.", Properties.Resources.theme),
			new("Rectify11 has better Performance", "We strongly value performance. You can choose things that you want to debloat in your system.", Properties.Resources.perf),
			new("Rectify11 has changed everything", "We have changed many icons in many different DLL's, resulting in a more consistent operating system.", Properties.Resources.ep),
			new("Rectified Control Panel", "We changed many details in the control panel, such as removing old gradients and adding back removed items", Properties.Resources.cp),
			new("Thank you!", "The team appreciates your support, thank you for installing Rectify11.", Properties.Resources.install)
		};
		#endregion
		#region Classes

		private class InstallerTexts
		{
			public string Title { get; set; }
			public string Description { get; set; }
			public Bitmap Side { get; set; }

			public InstallerTexts(string Title, string Description, Bitmap image)
			{
				this.Title = Title;
				this.Description = Description;
				Side = image;
			}
		}
		#endregion
		#region Public methods
		public ProgressPage(FrmWizard frm)
		{
			InitializeComponent();
			timer2 = new()
			{
				Interval = 1000
			};
			timer2.Tick += Timer2_Tick;
			frmwiz = frm;
		}
		public void StartReset()
		{
			timer1.Stop();
			progressText.Text = "Restarting your PC";
			progressInfo.Text = "Rectify11 has finished patching your system. Your PC needs to restart in order to apply the changes, it will automatically restart in " + duration.ToString() + " seconds";
			frmwiz.InstallerProgress = "Restarting in " + duration.ToString() + " seconds";
			frmwiz.UpdateSideImage = global::Rectify11Installer.Properties.Resources.incomplete;
			timer2.Start();
			frmwiz.ShowRebootButton = true;
			frmwiz.SetRebootHandler = rebootButton_Click;
		}
		public void Start()
		{
			timer1.Start();
			NextText();
		}
		#endregion
		#region Private Methods

		/// <summary>
		/// clears *.db cache files
		/// </summary>
		private async void ClearIconCache()
		{
			await Task.Run(() => Interaction.Shell("taskkill.exe /f /im explorer.exe", AppWinStyle.Hide, true));
			try
			{

				DirectoryInfo di = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "microsoft", "windows", "explorer"));
				var files = di.GetFiles("*.db");

				for (var i = 0; i < files.Length; i++)
				{
					files[i].Attributes = FileAttributes.Normal;
					if (File.Exists(files[i].FullName))
					{
						File.Delete(files[i].FullName);
					}
				}
				var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", true);
				if (key != null)
				{
					key.SetValue("ResetIconCache", Path.Combine(Variables.sys32Folder, "ie4uinit.exe") + " -show", RegistryValueKind.String);
				}
				key.Close();
			}
			catch
			{
				TaskDialog.Show(text: "deleting icon cache failed",
					title: "Rectify11 Setup",
					buttons: TaskDialogButtons.OK,
					icon: TaskDialogStandardIcon.Information);
			}
		}

		/// <summary>
		/// applies the theme just before restart to set the mouse cursor properly
		/// </summary>
		private async void ApplyScheme()
		{
			if (InstallOptions.InstallThemes)
			{
				var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", true);
				if (key != null)
				{
					if (InstallOptions.ThemeLight)
					{
						await Task.Run(() => Process.Start(Path.Combine(Variables.Windir, "Resources", "Themes", "lightrectified.theme")));
						key.SetValue("ApplyTheme", Path.Combine(Variables.Windir, "SecureUXHelper.exe") + " apply " + '"' + "Rectify11 light theme" + '"', RegistryValueKind.String);
					}
					else if (InstallOptions.ThemeDark)
					{
						await Task.Run(() => Process.Start(Path.Combine(Variables.Windir, "Resources", "Themes", "darkrectified.theme")));
						key.SetValue("ApplyTheme", Path.Combine(Variables.Windir, "SecureUXHelper.exe") + " apply " + '"' + "Rectify11 dark theme" + '"', RegistryValueKind.String);
					}
					else
					{
						await Task.Run(() => Process.Start(Path.Combine(Variables.Windir, "Resources", "Themes", "black.theme")));
						key.SetValue("ApplyTheme", Path.Combine(Variables.Windir, "SecureUXHelper.exe") + " apply " + '"' + "Rectify11 Dark Mica theme" + '"', RegistryValueKind.String);
					}
				}
				key.Close();
				using ShellLink shortcut = new();
				shortcut.Target = Path.Combine(Variables.r11Folder, "Rectify11ControlCenter", "Rectify11ControlCenter.exe");
				shortcut.WorkingDirectory = @"%windir%\Rectify11\Rectify11ControlCenter";
				shortcut.IconPath = Path.Combine(Variables.r11Folder, "Rectify11ControlCenter", "Rectify11ControlCenter.exe");
				shortcut.IconIndex = 0;
				shortcut.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
				shortcut.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Rectify11 Control Center.lnk"));
				shortcut.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Rectify11 Control Center.lnk"));
				shortcut.Dispose();
			}
		}

		/// <summary>
		/// goes to the next text in the list to be shown
		/// </summary>
		private void NextText()
		{
			CurrentTextIndex++;
			if (CurrentTextIndex >= Rectify11InstallerTexts.Length)
			{
				CurrentTextIndex = -1;
			}
			else
			{
				var t = Rectify11InstallerTexts[CurrentTextIndex];
				progressText.Text = t.Title;
				progressInfo.Text = t.Description;
				frmwiz.UpdateSideImage = t.Side;
			}
		}

		/// <summary>
		/// routine to perform before restarting
		/// </summary>
		private void RestartRoutine()
		{
			timer2.Stop();
			frmwiz.InstallerProgress = "Restarting...";
			ApplyScheme();
			ClearIconCache();
			Win32.NativeMethods.Reboot();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			NextText();
		}

		private async void rebootButton_Click(object sender, EventArgs e)
		{
			await Task.Run(() => RestartRoutine());
		}

		private async void Timer2_Tick(object sender, EventArgs e)
		{
			duration -= 1;
			frmwiz.InstallerProgress = "Restarting in " + duration.ToString() + " seconds";
			if (duration == 0)
			{
				await Task.Run(() => RestartRoutine());
			}
		}
		#endregion
	}
}
