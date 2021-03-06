﻿namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.Parsers.WebSearch;
    using RoliSoft.TVShowTracker.Parsers.WebSearch.Engines;

    /// <summary>
    /// Interaction logic for ProxiesSettings.xaml
    /// </summary>
    public partial class ProxiesSettings
    {
        /// <summary>
        /// Gets or sets the proxies list view item collection.
        /// </summary>
        /// <value>The proxies list view item collection.</value>
        public ObservableCollection<ProxiesListViewItem> ProxiesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the proxied domains list view item collection.
        /// </summary>
        /// <value>The proxied domains list view item collection.</value>
        public ObservableCollection<ProxiedDomainsListViewItem> ProxiedDomainsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxiesSettings"/> class.
        /// </summary>
        public ProxiesSettings()
        {
            InitializeComponent();
        }

        private bool _loaded;

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loaded) return;

            try
            {
                ProxiesListViewItemCollection = new ObservableCollection<ProxiesListViewItem>();
                proxiesListView.ItemsSource = ProxiesListViewItemCollection;

                ProxiedDomainsListViewItemCollection = new ObservableCollection<ProxiedDomainsListViewItem>();
                proxiedDomainsListView.ItemsSource = ProxiedDomainsListViewItemCollection;

                ReloadProxies();
            }
            catch (Exception ex)
            {
                MainWindow.HandleUnexpectedException(ex);
            }

            _loaded = true;

            ProxiesListViewSelectionChanged();
            ProxiedDomainsListViewSelectionChanged();
        }
        
        /// <summary>
        /// Handles the SelectionChanged event of the proxiesListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ProxiesListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            proxyEditButton.IsEnabled = proxySearchButton.IsEnabled = proxyTestButton.IsEnabled = proxyRemoveButton.IsEnabled = proxiesListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the proxiedDomainsListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ProxiedDomainsListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            proxyDomainEditButton.IsEnabled = proxyDomainRemoveButton.IsEnabled = proxiedDomainsListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Reloads the proxy-related list views.
        /// </summary>
        private void ReloadProxies()
        {
            ProxiesListViewItemCollection.Clear();

            foreach (var proxy in Settings.Get<Dictionary<string, object>>("Proxies"))
            {
                ProxiesListViewItemCollection.Add(new ProxiesListViewItem
                    {
                        Name    = proxy.Key,
                        Address = (string)proxy.Value
                    });
            }

            ProxiesListViewSelectionChanged();

            ProxiedDomainsListViewItemCollection.Clear();

            foreach (var proxy in Settings.Get<Dictionary<string, object>>("Proxied Domains"))
            {
                ProxiedDomainsListViewItemCollection.Add(new ProxiedDomainsListViewItem
                    {
                        Icon   = "http://g.etfv.co/http://www." + proxy.Key + "?defaulticon=lightpng",
                        Domain = proxy.Key,
                        Proxy  = (string)proxy.Value
                    });
            }

            ProxiedDomainsListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the Click event of the proxyAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyAddButtonClick(object sender, RoutedEventArgs e)
        {
            new ProxyWindow().ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;

            new ProxyWindow(sel.Name, sel.Address).ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxySearchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxySearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;
            
            Thread action = null;
            var done = false;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;
            var uri = new Uri(sel.Address.Replace("$domain.", string.Empty));

            if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
            {
                var app = "a local application";

                try
                {
                    var tcpRows = Utils.GetExtendedTCPTable();
                    foreach (var row in tcpRows)
                    {
                        if (((row.localPort1 << 8) + (row.localPort2) + (row.localPort3 << 24) + (row.localPort4 << 16)) == uri.Port)
                        {
                            app = "PID " + row.owningPid + " (" + Process.GetProcessById(row.owningPid).Modules[0].FileName + ")";
                            break;
                        }
                    }
                }
                catch { }

                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon        = VistaTaskDialogIcon.Warning,
                        Title           = sel.Name,
                        MainInstruction = "Potentially dangerous",
                        Content         = "This proxy points to a local loopback address on port " + uri.Port + ".\r\nYour requests will go to " + app + ", which will most likely forward them to an external server.",
                        CustomButtons   = new[] { "OK" }
                    });
                return;
            }
            
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = sel.Name,
                    MainInstruction         = "Testing proxy",
                    Content                 = "Testing whether " + uri.Host + " is a known proxy...",
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp)
                            {
                                dialog.SetProgressBarMarquee(true, 0);
                                showmbp = true;
                            }

                            if (args.ButtonId != 0)
                            {
                                if (!done)
                                {
                                    try { action.Abort(); } catch { }
                                }

                                return false;
                            }

                            if (done)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();

            action = new Thread(() =>
                {
                    try
                    {
                        var src = new DuckDuckGo();
                        var res = new List<SearchResult>();
                        res.AddRange(src.Search("\"" + uri.Host + "\" intitle:proxy"));

                        if (res.Count == 0)
                        {
                            res.AddRange(src.Search("\"" + uri.Host + "\" intitle:proxies"));
                        }

                        done = true;

                        if (res.Count == 0)
                        {
                            TaskDialog.Show(new TaskDialogOptions
                                {
                                    MainIcon        = VistaTaskDialogIcon.Information,
                                    Title           = sel.Name,
                                    MainInstruction = "Not a known public proxy",
                                    Content         = uri.Host + " does not seem to be a known public proxy." + Environment.NewLine + Environment.NewLine +
                                                      "If your goal is to trick proxy detectors, you're probably safe for now. However, you shouldn't use public proxies if you don't want to potentially compromise your account.",
                                    CustomButtons   = new[] { "OK" }
                                });
                            return;
                        }
                        else
                        {
                            TaskDialog.Show(new TaskDialogOptions
                                {
                                    MainIcon        = VistaTaskDialogIcon.Error,
                                    Title           = sel.Name,
                                    MainInstruction = "Known public proxy",
                                    Content         = uri.Host + " is a known public proxy according to " + new Uri(res[0].URL).Host.Replace("www.", string.Empty) + Environment.NewLine + Environment.NewLine +
                                                      "If the site you're trying to access through this proxy forbids proxy usage, they're most likely use a detection mechanism too, which will trigger an alert when it sees this IP address. Your requests will be denied and your account might also get banned. Even if the site's detector won't recognize it, using a public proxy is not such a good idea, because you could compromise your account as public proxy operators are known to be evil sometimes.",
                                    CustomButtons   = new[] { "OK" }
                                });
                            return;
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Error,
                                Title           = sel.Name,
                                MainInstruction = "Connection error",
                                Content         = "An error occurred while checking the proxy.",
                                ExpandedInfo    = ex.Message,
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the proxyTestButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyTestButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            Thread action = null;
            var done = false;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;
            var uri = new Uri(sel.Address.Replace("$domain.", string.Empty));

            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = sel.Name,
                    MainInstruction         = "Testing proxy",
                    Content                 = "Testing connection through " + uri.Host + ":" + uri.Port + "...",
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp)
                            {
                                dialog.SetProgressBarMarquee(true, 0);
                                showmbp = true;
                            }

                            if (args.ButtonId != 0)
                            {
                                if (!done)
                                {
                                    try { action.Abort(); } catch { }
                                }

                                return false;
                            }

                            if (done)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();
            
            action = new Thread(() =>
                {
                    var s = Stopwatch.StartNew();

                    try
                    {
                        var b = Utils.GetHTML("http://rolisoft.net/b", proxy: sel.Address);
                        s.Stop();

                        done = true;

                        var tor  = b.DocumentNode.SelectSingleNode("//img[@class='tor']");
                        var ip   = b.DocumentNode.GetTextValue("//span[@class='ip'][1]");
                        var host = b.DocumentNode.GetTextValue("//span[@class='host'][1]");
                        var geo  = b.DocumentNode.GetTextValue("//span[@class='geoip'][1]");

                        if (tor != null)
                        {
                            TaskDialog.Show(new TaskDialogOptions
                                {
                                    MainIcon        = VistaTaskDialogIcon.Error,
                                    Title           = sel.Name,
                                    MainInstruction = "TOR detected",
                                    Content         = ip + " is a TOR exit node." + Environment.NewLine + Environment.NewLine +
                                                      "If the site you're trying to access through this proxy forbids proxy usage, they're most likely use a detection mechanism too, which will trigger an alert when it sees this IP address. Your requests will be denied and your account might also get banned. Even if the site's detector won't recognize it, using TOR is not such a good idea, because you could compromise your account as TOR exit node operators are known to be evil sometimes.",
                                    CustomButtons   = new[] { "OK" }
                                });
                        }

                        if (ip == null)
                        {
                            TaskDialog.Show(new TaskDialogOptions
                                {
                                    MainIcon        = VistaTaskDialogIcon.Error,
                                    Title           = sel.Name,
                                    MainInstruction = "Proxy error",
                                    Content         = "The proxy did not return the requested resource, or greatly modified the structure of the page. Either way, it is not suitable for use with this software.",
                                    CustomButtons   = new[] { "OK" }
                                });
                            return;
                        }

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Information,
                                Title           = sel.Name,
                                MainInstruction = "Test results",
                                Content         = "Total time to get rolisoft.net/b: " + s.Elapsed + "\r\n\r\nIP address: " + ip + "\r\nHost name: " + host + "\r\nGeoIP lookup: " + geo,
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        TaskDialog.Show(new TaskDialogOptions
                            {
                                MainIcon        = VistaTaskDialogIcon.Error,
                                Title           = sel.Name,
                                MainInstruction = "Connection error",
                                Content         = "An error occurred while connecting to the proxy.",
                                ExpandedInfo    = ex.Message,
                                CustomButtons   = new[] { "OK" }
                            });
                    }
                    finally
                    {
                        if (s.IsRunning)
                        {
                            s.Stop();
                        }
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the proxyRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Name + " and all the proxied domains associated with it?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var dict = Settings.Get<Dictionary<string, object>>("Proxied Domains");

                foreach (var prdmn in dict.ToDictionary(k => k.Key, v => v.Value))
                {
                    if ((string)prdmn.Value == sel.Name)
                    {
                        dict.Remove(prdmn.Key);
                    }
                }

                Settings.Get<Dictionary<string, object>>("Proxies").Remove(sel.Name);
                Settings.Save();

                ReloadProxies();
            }
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainAddButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.Items.Count == 0)
            {
                MessageBox.Show("You need to add a new proxy before adding domains.", "No proxies", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            new ProxiedDomainWindow().ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiedDomainsListView.SelectedIndex == -1) return;

            if (proxiesListView.Items.Count == 0)
            {
                MessageBox.Show("You need to add a new proxy before adding domains.", "No proxies", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sel = (ProxiedDomainsListViewItem)proxiedDomainsListView.SelectedItem;

            new ProxiedDomainWindow(sel.Domain, sel.Proxy).ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiedDomainsListView.SelectedIndex == -1) return;

            var sel = (ProxiedDomainsListViewItem)proxiedDomainsListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Domain + "?", "Remove " + sel.Domain, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Settings.Get<Dictionary<string, object>>("Proxied Domains").Remove(sel.Domain);
                Settings.Save();

                ReloadProxies();
            }
        }
    }
}
