<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookmarkTreeView.ascx.cs" Inherits="RockWeb.Blocks.Core.BookmarkTreeView" %>

<asp:UpdatePanel ID="upCategoryTree" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="false">
    <ContentTemplate>

        <asp:HiddenField ID="hfInitialCategoryParentIds" runat="server" />
        <asp:HiddenField ID="hfSelectedItemId" runat="server" />

        <div class="treeview js-categorytreeview">

            <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

            <div class="treeview-scroll scroll-container scroll-container-horizontal">

                <div class="viewport">
                    <div class="overview">
                        <div class="panel-body treeview-frame">
                            <asp:Panel ID="pnlTreeviewContent" runat="server" />
                        </div>
                    </div>
                </div>

                <div class="scrollbar">
                    <div class="track">
                        <div class="thumb">
                            <div class="end"></div>
                        </div>
                    </div>
                </div>
            </div>

        </div>

        <script type="text/javascript">

            var scrollbCategory = $('#<%=pnlTreeviewContent.ClientID%>').closest('.treeview-scroll');
            scrollbCategory.tinyscrollbar({ axis: 'x', sizethumb: 60, size: 200 });

            // resize scrollbar when the window resizes
            $(document).ready(function () {
                $(window).on('resize', function () {
                    resizeScrollbar(scrollbCategory);
                });
            });

            // scrollbar hide/show
            var timerScrollHide;
            $("#<%=upCategoryTree.ClientID%>").on({
                mouseenter: function () {
                    clearTimeout(timerScrollHide);
                    $("[id$='upCategoryTree'] div[class~='scrollbar'] div[class='track'").fadeIn('fast');
                },
                mouseleave: function () {
                    timerScrollHide = setTimeout(function () {
                        $("[id$='upCategoryTree'] div[class~='scrollbar'] div[class='track'").fadeOut('slow');
                    }, 1000);
                }
            });

            if ('<%= RestParms %>' == '') {
                // EntityType not set
                $('#<%=pnlTreeviewContent.ClientID%>').hide();
            }

            $(function () {
                var $selectedId = $('#<%=hfSelectedItemId.ClientID%>'),
                    $expandedIds = $('#<%=hfInitialCategoryParentIds.ClientID%>'),
                    _mapCategories = function (arr) {
                        return $.map(arr, function (item) {
                            var node = {
                                id: item.Guid || item.Id,
                                name: item.Name || item.Title,
                                iconCssClass: item.IconCssClass,
                                parentId: item.ParentId,
                                hasChildren: item.HasChildren,
                                isCategory: item.IsCategory,
                                isActive: item.IsActive,
                                entityId: item.Id
                            };

                            // If this Tree Node represents a Category, add a prefix to its identifier to prevent collisions with other Entity identifiers.
                            if (item.IsCategory) {
                                node.id = 'C' + node.id;
                            }
                            if (item.Children && typeof item.Children.length === 'number') {
                                node.children = _mapCategories(item.Children);
                            }

                            return node;
                        });
                    };

                $('#<%=pnlTreeviewContent.ClientID%>')
                    .on('rockTree:selected', function (e, id) {

                        var $node = $('[data-id="' + id + '"]'),
                            isCategory = $node.attr('data-iscategory') === 'true';

                        var locationUrl = null;
                        if (!isCategory) {
                            $.ajax({
                                type: 'GET',
                                contentType: 'application/json',
                                dataType: 'json',
                                url: Rock.settings.get('baseUrl') + 'api/PersonBookmarks/' + id,
                                success: function (getData, status, xhr) {
                                    if (getData) {
                                        if (getData.Url) {
                                            window.location = getData.Url;
                                        }
                                    }
                                },
                                error: function (xhr, status, error) {
                                    alert(status + ' [' + error + ']: ' + xhr.responseText);
                                }
                            });
                        }
                    })
                    .on('rockTree:rendered', function () {

                        // update viewport height
                        resizeScrollbar(scrollbCategory);

                    })
                    .rockTree({
                        restUrl: '<%= ResolveUrl( "~/api/categories/getchildren/" ) %>',
                            restParams: '<%= RestParms %>',
                            mapping: {
                                include: ['isCategory', 'entityId'],
                                mapData: _mapCategories
                            },
                            selectedIds: $selectedId.val() ? $selectedId.val().split(',') : null,
                            expandedIds: $expandedIds.val() ? $expandedIds.val().split(',') : null
                    });
            });

            function resizeScrollbar(scrollControl) {
                var overviewHeight = $(scrollControl).find('.overview').height();

                $(scrollControl).find('.viewport').height(overviewHeight);

                scrollControl.tinyscrollbar_update('relative');
            }


        </script>

    </ContentTemplate>
</asp:UpdatePanel>
