using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using AdditionalSettings = Rock.Rest.Controllers.MobileController.AdditionalSettings;
using ShellType = Rock.Rest.Controllers.MobileController.ShellType;
using TabLocation = Rock.Rest.Controllers.MobileController.TabLocation;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Security;

namespace RockWeb.Blocks.Mobile
{
    [DisplayName( "Mobile Application Detail" )]
    [Category( "Mobile" )]
    [Description( "Edits and configures the settings of a mobile application." )]
    [LinkedPage( "Layout Detail", "", true ) ]
    [LinkedPage( "Page Detail", "", true )]
    public partial class MobileApplicationDetail : RockBlock
    {
        private static class AttributeKeys
        {
            public const string LayoutDetail = "LayoutDetail";

            public const string PageDetail = "PageDetail";
        }

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gLayouts.Actions.ShowMergeTemplate = false;
            gLayouts.Actions.ShowExcelExport = false;
            gLayouts.Actions.AddClick += gLayouts_AddClick;
            gLayouts.Actions.ShowAdd = true;
            gLayouts.DataKeyNames = new[] { "Id" };

            gPages.Actions.ShowMergeTemplate = false;
            gPages.Actions.ShowExcelExport = false;
            gPages.Actions.AddClick += gPages_AddClick;
            gPages.Actions.ShowAdd = true;
            gPages.DataKeyNames = new[] { "Id" };
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                ConfigureControls();

                var siteId = PageParameter( "SiteId" ).AsInteger();

                if ( siteId != 0 )
                {
                    ShowDetail( siteId );
                }
                else
                {
                    ShowEdit( siteId );
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Configures the controls.
        /// </summary>
        private void ConfigureControls()
        {
            imgEditIcon.BinaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT.AsGuid();
            imgEditPreviewThumbnail.BinaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT.AsGuid();

            rblEditApplicationType.BindToEnum<ShellType>();
            rblEditAndroidTabLocation.BindToEnum<TabLocation>();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        private void ShowDetail( int siteId )
        {
            var site = new SiteService( new RockContext() ).Get( siteId );

            //
            // Make sure the site exists.
            //
            if ( site == null )
            {
                nbError.Text = "That mobile application does not exist.";
                pnlOverview.Visible = false;

                return;
            }

            //
            // Ensure user is authorized to view mobile sites.
            //
            if ( !IsUserAuthorized( Authorization.VIEW ) )
            {
                nbError.Text = Rock.Constants.EditModeMessage.NotAuthorizedToView( "mobile application" );
                pnlOverview.Visible = false;

                return;
            }

            //
            // Ensure this is a mobile site.
            //
            if ( site.SiteType != SiteType.Mobile )
            {
                nbError.Text = "This block only supports mobile sites.";
                pnlOverview.Visible = false;

                return;
            }

            //
            // Set the UI fields for the standard values.
            //
            hfSiteId.Value = site.Id.ToString();
            ltAppName.Text = site.Name.EncodeHtml();
            ltDescription.Text = site.Description.EncodeHtml();

            //
            // Set the UI fields for the images.
            //
            imgAppIcon.ImageUrl = string.Format( "~/GetImage.ashx?Id={0}", site.SiteLogoBinaryFileId );
            imgAppIcon.Visible = site.SiteLogoBinaryFileId.HasValue;
            imgAppPreview.ImageUrl = string.Format( "~/GetImage.ashx?Id={0}", site.ThumbnailFileId );
            imgAppPreview.Visible = site.ThumbnailFileId.HasValue;

            //
            // Set the UI fields for the additional details.
            //
            var additionalSettings = site.AdditionalSettings.FromJsonOrNull<AdditionalSettings>() ?? new AdditionalSettings();
            var fields = new List<KeyValuePair<string, string>>();

            if ( additionalSettings.ShellType.HasValue )
            {
                fields.Add( new KeyValuePair<string, string>( "Application Type", additionalSettings.ShellType.ToString() ) );
            }

            // TODO: I'm pretty sure something like this already exists in Rock, but I can never find it. - dh
            ltAppDetails.Text = string.Join( "", fields.Select( f => string.Format( "<dl><dt>{0}</dt><dd>{1}</dd></dl>", f.Key, f.Value ) ) );

            //
            // Bind the grids.
            //
            BindLayouts( siteId );
            BindPages( siteId );

            pnlContent.Visible = true;
            pnlOverview.Visible = true;
            pnlEdit.Visible = false;

            //
            // If we are returning from a child page, make sure the correct tab is selected.
            //
            if ( PageParameter( "Tab" ) == "Layouts" )
            {
                ShowLayoutsTab();
            }
            else if (PageParameter( "Tab" ) == "Pages" )
            {
                ShowPagesTab();
            }
            else
            {
                ShowApplicationTab();
            }
        }

        /// <summary>
        /// Shows the application tab.
        /// </summary>
        private void ShowApplicationTab()
        {
            liTabApplication.AddCssClass( "active" );
            liTabLayouts.RemoveCssClass( "active" );
            liTabPages.RemoveCssClass( "active" );

            pnlApplication.Visible = true;
            pnlLayouts.Visible = false;
            pnlPages.Visible = false;
        }

        /// <summary>
        /// Shows the layouts tab.
        /// </summary>
        private void ShowLayoutsTab()
        {
            liTabApplication.RemoveCssClass( "active" );
            liTabLayouts.AddCssClass( "active" );
            liTabPages.RemoveCssClass( "active" );

            pnlApplication.Visible = false;
            pnlLayouts.Visible = true;
            pnlPages.Visible = false;
        }

        /// <summary>
        /// Shows the pages tab.
        /// </summary>
        private void ShowPagesTab()
        {
            liTabApplication.RemoveCssClass( "active" );
            liTabLayouts.RemoveCssClass( "active" );
            liTabPages.AddCssClass( "active" );

            pnlApplication.Visible = false;
            pnlLayouts.Visible = false;
            pnlPages.Visible = true;
        }

        /// <summary>
        /// Shows the edit.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        private void ShowEdit( int siteId )
        {
            var site = new SiteService( new RockContext() ).Get( siteId );
            AdditionalSettings additionalSettings;

            //
            // Ensure user can edit the mobile site.
            //
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                nbError.Text = Rock.Constants.EditModeMessage.NotAuthorizedToEdit( "mobile application" );
                pnlOverview.Visible = false;

                return;
            }

            //
            // If we are generating a new site, set the initial values.
            //
            if ( site == null )
            {
                site = new Site
                {
                    IsActive = true,
                    AdditionalSettings = new AdditionalSettings
                    {
                        ShellType = ShellType.Flyout,
                        TabLocation = TabLocation.Bottom,
                        CssStyle = string.Empty
                    }.ToJson()
                };
            }

            //
            // Decode our additional site settings.
            //
            if ( site.AdditionalSettings != null )
            {
                additionalSettings = site.AdditionalSettings.FromJsonOrNull<AdditionalSettings>() ?? new AdditionalSettings();
            }
            else
            {
                additionalSettings = new AdditionalSettings();
            }

            //
            // Set basic UI fields.
            //
            tbEditName.Text = site.Name;
            cbEditActive.Checked = site.IsActive;
            tbEditDescription.Text = site.Description;

            rblEditApplicationType.SetValue( ( int? ) additionalSettings.ShellType ?? ( int ) ShellType.Flyout );
            rblEditAndroidTabLocation.SetValue( ( int? ) additionalSettings.TabLocation ?? ( int ) TabLocation.Bottom );
            ceEditCssStyles.Text = additionalSettings.CssStyle ?? string.Empty;

            rblEditAndroidTabLocation.Visible = rblEditApplicationType.SelectedValueAsInt() == ( int ) ShellType.Tabbed;

            //
            // Set image UI fields.
            //
            imgEditIcon.BinaryFileId = site.SiteLogoBinaryFileId;
            imgEditPreviewThumbnail.BinaryFileId = site.ThumbnailFileId;

            pnlContent.Visible = false;
            pnlEdit.Visible = true;
        }

        /// <summary>
        /// Binds the layouts.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        private void BindLayouts( int siteId )
        {
            var layouts = LayoutCache.All()
                .Where( l => l.SiteId == siteId )
                .OrderBy( l => l.Name )
                .ToList();

            gLayouts.DataSource = layouts;
            gLayouts.DataBind();
        }

        /// <summary>
        /// Binds the pages.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        private void BindPages( int siteId )
        {
            var pages = PageCache.All()
                .Where( p => p.SiteId == siteId )
                .OrderBy( p => p.Order )
                .ThenBy( p => p.InternalName )
                .ToList();

            gPages.DataSource = pages;
            gPages.DataBind();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the lbEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEdit_Click( object sender, EventArgs e )
        {
            ShowEdit( hfSiteId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the Click event of the lbEditCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEditCancel_Click( object sender, EventArgs e )
        {
            var siteId = PageParameter( "SiteId" ).AsInteger();

            if ( siteId == 0 )
            {
                NavigateToParentPage();
            }
            else
            {
                ShowDetail( siteId );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbEditSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEditSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var siteService = new SiteService( rockContext );
            var binaryFileService = new BinaryFileService( rockContext );

            //
            // Find the site or if we are creating a new one, bootstrap it.
            //
            var site = siteService.Get( PageParameter( "SiteId" ).AsInteger() );
            if ( site == null )
            {
                site = new Site
                {
                    SiteType = SiteType.Mobile
                };
                siteService.Add( site );
            }

            //
            // Save the basic settings.
            //
            site.Name = tbEditName.Text;
            site.IsActive = cbEditActive.Checked;
            site.Description = tbEditDescription.Text;

            //
            // Save the additional settings.
            //
            var additionalSettings = new AdditionalSettings
            {
                ShellType = rblEditApplicationType.SelectedValueAsEnum<ShellType>(),
                TabLocation = rblEditAndroidTabLocation.SelectedValueAsEnum<TabLocation>(),
                CssStyle = ceEditCssStyles.Text
            };

            site.AdditionalSettings = additionalSettings.ToJson();

            //
            // Save the images.
            //
            site.SiteLogoBinaryFileId = imgEditIcon.BinaryFileId;
            site.ThumbnailFileId = imgEditPreviewThumbnail.BinaryFileId;

            //
            // Ensure the images are persisted.
            //
            if ( site.SiteLogoBinaryFileId.HasValue )
            {
                binaryFileService.Get( site.SiteLogoBinaryFileId.Value ).IsTemporary = false;
            }
            if ( site.ThumbnailFileId.HasValue )
            {
                binaryFileService.Get( site.ThumbnailFileId.Value ).IsTemporary = false;
            }

            rockContext.SaveChanges();

            NavigateToCurrentPage( new Dictionary<string, string>
            {
                { "SiteId", site.Id.ToString() }
            } );
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblEditApplicationType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblEditApplicationType_SelectedIndexChanged( object sender, EventArgs e )
        {
            rblEditAndroidTabLocation.Visible = rblEditApplicationType.SelectedValueAsInt() == ( int ) ShellType.Tabbed;
        }

        /// <summary>
        /// Handles the Click event of the lbTabApplication control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbTabApplication_Click( object sender, EventArgs e )
        {
            ShowApplicationTab();
        }

        /// <summary>
        /// Handles the Click event of the lbTabLayouts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbTabLayouts_Click( object sender, EventArgs e )
        {
            ShowLayoutsTab();
        }

        /// <summary>
        /// Handles the Click event of the lbTabPages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbTabPages_Click( object sender, EventArgs e )
        {
            ShowPagesTab();
        }

        #endregion

        #region Layouts Grid Event Handlers

        /// <summary>
        /// Handles the AddClick event of the gLayouts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gLayouts_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.LayoutDetail, new Dictionary<string, string>
            {
                { "SiteId", hfSiteId.Value },
                { "LayoutId", "0" }
            } );
        }

        /// <summary>
        /// Handles the RowSelected event of the gLayouts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gLayouts_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.LayoutDetail, new Dictionary<string, string>
            {
                { "SiteId", hfSiteId.Value },
                { "LayoutId", e.RowKeyId.ToString() }
            } );
        }

        /// <summary>
        /// Handles the GridRebind event of the gLayouts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs"/> instance containing the event data.</param>
        protected void gLayouts_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindLayouts( hfSiteId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the DeleteClick event of the gLayouts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gLayouts_DeleteClick( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var rockContext = new RockContext();
            var layoutService = new LayoutService( rockContext );
            var layout = layoutService.Get( e.RowKeyId );

            layoutService.Delete( layout );

            rockContext.SaveChanges();

            BindLayouts( hfSiteId.ValueAsInt() );
        }

        #endregion

        #region Pages Grid Event Handlers

        /// <summary>
        /// Handles the AddClick event of the gPages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gPages_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.PageDetail, new Dictionary<string, string>
            {
                { "SiteId", hfSiteId.Value },
                { "Page", "0" }
            } );
        }

        /// <summary>
        /// Handles the RowSelected event of the gPages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gPages_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.PageDetail, new Dictionary<string, string>
            {
                { "SiteId", hfSiteId.Value },
                { "Page", e.RowKeyId.ToString() }
            } );
        }

        /// <summary>
        /// Handles the GridRebind event of the gPages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs"/> instance containing the event data.</param>
        protected void gPages_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindPages( hfSiteId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the DeleteClick event of the gPages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gPages_DeleteClick( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var rockContext = new RockContext();
            var pageService = new PageService( rockContext );
            var page = pageService.Get( e.RowKeyId );

            pageService.Delete( page );

            rockContext.SaveChanges();

            BindPages( hfSiteId.ValueAsInt() );
        }

        #endregion
    }
}
