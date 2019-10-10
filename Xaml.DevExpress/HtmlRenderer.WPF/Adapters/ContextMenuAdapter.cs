// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Windows;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.WPF.Utilities;

namespace TheArtOfDev.HtmlRenderer.WPF.Adapters
{
	using DevExpress.Xpf.Bars;

	/// <summary>
    /// Adapter for WPF context menu for core.
    /// </summary>
    internal sealed class ContextMenuAdapter : RContextMenu
    {
        #region Fields and Consts

        /// <summary>
        /// the underline WPF context menu
        /// </summary>
        private readonly PopupMenu _contextMenu;

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        public ContextMenuAdapter()
        {
            _contextMenu = new PopupMenu
            {
				GlyphSize = GlyphSize.Custom,
				CustomGlyphSize = default,
            };
        }

        public override int ItemsCount => _contextMenu.Items.Count;

        public override void AddDivider()
        {
            _contextMenu.Items.Add(new BarItemSeparator());
        }

        public override void AddItem(string text, bool enabled, EventHandler onClick)
        {
            ArgChecker.AssertArgNotNullOrEmpty(text, "text");
            ArgChecker.AssertArgNotNull(onClick, "onClick");

            var item = new BarButtonItem
            {
	            Content = text,
	            IsEnabled = enabled
            };
            item.ItemClick += new ItemClickEventHandler(onClick);
            _contextMenu.Items.Add(item);
        }

        public override void RemoveLastDivider()
        {
            if (_contextMenu.Items[_contextMenu.Items.Count - 1] is BarItemSeparator)
                _contextMenu.Items.RemoveAt(_contextMenu.Items.Count - 1);
        }

        public override void Show(RControl parent, RPoint location)
        {
            _contextMenu.PlacementTarget = ((ControlAdapter)parent).Control;
            _contextMenu.PlacementRectangle = new Rect(Utils.ConvertRound(location), Size.Empty);
            _contextMenu.IsOpen = true;
        }

        public override void Dispose()
        {
            _contextMenu.IsOpen = false;
            _contextMenu.PlacementTarget = null;
            _contextMenu.Items.Clear();
        }
    }
}