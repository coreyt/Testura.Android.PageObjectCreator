﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Testura.Android.Device.Ui.Nodes.Data;
using Testura.Android.PageObjectCreator.Models;
using Testura.Android.PageObjectCreator.Models.Messages;
using Testura.Android.PageObjectCreator.Services;
using Testura.Android.PageObjectCreator.Util.Extensions;
using Testura.Android.Util;

namespace Testura.Android.PageObjectCreator.ViewModels
{
    [ImplementPropertyChanged]
    public class ScreenViewModel : ViewModelBase
    {
        private readonly IScreenService _screenService;
        private readonly IDialogService _dialogService;
        private readonly IUniqueWithFinderService _uniqueWithFinderService;
        private Node _topNode;

        public ScreenViewModel(IScreenService screenService, IDialogService dialogService, IUniqueWithFinderService uniqueWithFinderService)
        {
            _screenService = screenService;
            _dialogService = dialogService;
            _uniqueWithFinderService = uniqueWithFinderService;
            ShouldShowInfoMessage = true;
            MessengerInstance.Register<DumpMessage>(this, OnNewDump);
            MessengerInstance.Register<StartedDumpScreenMessage>(this, OnStartedDumpingScreen);
            MessengerInstance.Register<StoppedDumpScreenMessage>(this, OnStoppedDumpingScreen);
            MessengerInstance.Register<SelectedHierarchyNodeMesssage>(this, OnSelectedHierarchyNode);
            MessengerInstance.Register<AddNodeMessage>(this, OnAddNode);
            MessengerInstance.Register<UiObjectInfoRemovedMessage>(this, OnUiObjectInfoRemoved);
        }

        public event EventHandler<Node> NewTemporaryHierarchyNode;

        public event EventHandler<Node> HierarchyNodeAdded;

        public event EventHandler<Node> NodeRemoved;

        public event EventHandler<AndroidDumpInfo> LoadImage;

        public bool IsDumpingScreen { get; set; }

        public bool ShouldShowInfoMessage { get; set; }

        public Node GetNodes(Point point, string dumpPath)
        {
            var nodes = _screenService.GetNodes(point, _topNode.AllNodes());
            if (nodes.Any())
            {
                var node = nodes.First();

                // Sometimes we get nodes that contains nodes with the same boundary
                // So this is just to make sure we get the absolut child.
                while (node.Children.Any())
                {
                    node = node.Children.First();
                }

                return node;
            }

            return null;
        }

        public bool AddNode(Node node)
        {
            var name = _dialogService.ShowNameDialog();
            if (!string.IsNullOrEmpty(name))
            {
                var uiNodeInfo = new UiObjectInfo
                {
                    Name = name,
                    Node = node,
                    FindWith = new List<AttributeTags>(),
                    AutoSelectedWith = _uniqueWithFinderService.GetUniqueWiths(node, _topNode.AllNodes(), false),
                    DisplayName = "Automatic"
                };
                MessengerInstance.Send(new AddUiObjectInfoMessage { UiNodeInfo = uiNodeInfo });
                return true;
            }

            return false;
        }

        public void ShowNodeDetails(Node node)
        {
            MessengerInstance.Send(new ShowNodeDetailsMessage { Node = node });
        }

        private void OnAddNode(AddNodeMessage message)
        {
            if (AddNode(message.Node))
            {
                HierarchyNodeAdded?.Invoke(this, message.Node);
            }
        }

        private void OnNewDump(DumpMessage message)
        {
            IsDumpingScreen = false;
            LoadImage?.Invoke(this, message.DumpInfo);
            _topNode = message.TopNode;
        }

        private void OnStartedDumpingScreen(StartedDumpScreenMessage message)
        {
            IsDumpingScreen = true;
            ShouldShowInfoMessage = false;
        }

        private void OnStoppedDumpingScreen(StoppedDumpScreenMessage messsage)
        {
            IsDumpingScreen = false;
        }

        private void OnSelectedHierarchyNode(SelectedHierarchyNodeMesssage message)
        {
            NewTemporaryHierarchyNode?.Invoke(this, message.SelectedNode);
        }

        private void OnUiObjectInfoRemoved(UiObjectInfoRemovedMessage message)
        {
            NodeRemoved?.Invoke(this, message.UiObjectInfo.Node);
        }
    }
}
