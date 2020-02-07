﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace V2RayGCon.Controllers.CoreServerComponent
{
    public class CoreStates :
        VgcApis.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Interfaces.CoreCtrlComponents.ICoreStates
    {
        VgcApis.Models.Datas.CoreInfo coreInfo;
        Services.Servers servers;

        public CoreStates(
            Services.Servers servers,
            VgcApis.Models.Datas.CoreInfo coreInfo)
        {
            this.servers = servers;
            this.coreInfo = coreInfo;
        }

        CoreCtrl coreCtrl;
        Configer configer;
        public override void Prepare()
        {
            coreCtrl = GetSibling<CoreCtrl>();
            configer = GetSibling<Configer>();
        }

        #region properties



        #endregion

        #region public methods
        string GetInProtocolNameByNumber(int typeNumber)
        {
            var table = Models.Datas.Table.customInbTypeNames;
            return table[Misc.Utils.Clamp(typeNumber, 0, table.Length)];
        }

        public void SetIndexQuiet(double index) => SetIndexWorker(index, true);

        public void SetIndex(double index) => SetIndexWorker(index, false);

        void SetIndexWorker(double index, bool quiet)
        {
            if (Misc.Utils.AreEqual(coreInfo.index, index))
            {
                return;
            }

            coreInfo.index = index;
            coreCtrl.SetTitle(GetTitle());
            if (!quiet)
            {
                GetParent().InvokeEventOnPropertyChange();
            }
        }

        public bool GetterInfoForSearch(Func<string[], bool> filter)
        {
            var ci = coreInfo;
            return filter(new string[] {

                // index 0
                // use title instead of name + summary
                GetTitle(),
                // ci.name+ci.summary,
                
                // index 1
                GetInProtocolNameByNumber(ci.customInbType)
                + ci.inbIp + @":" + ci.inbPort.ToString(),

                // index 2
                ci.customMark??"",
            });
        }

        public void SetIsInjectSkipCnSite(bool isInjectSkipCnSite)
        {
            SetSettingsPropertyOnDemand(ref coreInfo.isInjectSkipCNSite, true);
        }

        public void SetIsAutoRun(bool isAutoRun) =>
            SetSettingsPropertyOnDemand(ref coreInfo.isAutoRun, isAutoRun);

        public void SetIsUntrack(bool isUntrack) =>
            SetSettingsPropertyOnDemand(ref coreInfo.isUntrack, isUntrack);

        public void SetIsInjectImport(bool IsInjectImport)
        {
            SetSettingsPropertyOnDemand(ref coreInfo.isInjectImport, IsInjectImport, true);
            configer.UpdateSummaryThen(() => GetParent().InvokeEventOnPropertyChange());
        }

        public VgcApis.Models.Datas.CoreInfo GetAllRawCoreInfo() => coreInfo;

        readonly object genUidLocker = new object();
        public string GetUid()
        {
            lock (genUidLocker)
            {
                if (string.IsNullOrEmpty(coreInfo.uid))
                {
                    var uidList = servers
                        .GetAllServersOrderByIndex()
                        .Select(s => s.GetCoreStates().GetRawUid())
                        .ToList();

                    string newUid;
                    do
                    {
                        newUid = Misc.Utils.RandomHex(16);
                    } while (uidList.Contains(newUid));

                    coreInfo.uid = newUid;
                    GetParent().InvokeEventOnPropertyChange();
                }
            }
            return coreInfo.uid;
        }

        public double GetIndex() => coreInfo.index;

        public string GetMark() => coreInfo.customMark;

        public string GetSummary() => coreInfo.summary ?? @"";

        public long GetLastModifiedUtcTicks() => coreInfo.lastModifiedUtcTicks;

        public void SetLastModifiedUtcTicks(long utcTicks) =>
            SetPropertyOnDemand(ref coreInfo.lastModifiedUtcTicks, utcTicks);

        public void SetIsSelected(bool selected)
        {
            if (selected == coreInfo.isSelected)
            {
                return;
            }
            coreInfo.isSelected = selected;
            GetParent().InvokeEventOnPropertyChange();
        }

        public void SetInboundAddr(string ip, int port)
        {
            var changed = false;

            if (ip != coreInfo.inbIp)
            {
                coreInfo.inbIp = ip;
                changed = true;
            }

            if (port != coreInfo.inbPort)
            {
                coreInfo.inbPort = port;
                changed = true;

            }

            if (changed)
            {
                GetParent().InvokeEventOnPropertyChange();
            }
        }

        public void SetInboundType(int type)
        {
            if (coreInfo.customInbType == type)
            {
                return;
            }

            coreInfo.customInbType = Misc.Utils.Clamp(
                type, 0, Models.Datas.Table.customInbTypeNames.Length);

            GetParent().InvokeEventOnPropertyChange();
            if (coreCtrl.IsCoreRunning())
            {
                coreCtrl.RestartCoreThen();
            }
        }

        public int GetInboundType() => coreInfo.customInbType;
        public string GetInboundAddr() =>
                    $"{coreInfo.inbIp}:{coreInfo.inbPort}";

        public void SetMark(string mark)
        {
            if (coreInfo.customMark == mark)
            {
                return;
            }

            coreInfo.customMark = mark;
            servers.AddNewMark(mark);
            GetParent().InvokeEventOnPropertyChange();
        }

        public string GetInboundIp() => coreInfo.inbIp;
        public int GetInboundPort() => coreInfo.inbPort;

        public bool IsAutoRun() => coreInfo.isAutoRun;
        public bool IsSelected() => coreInfo.isSelected;
        public bool IsUntrack() => coreInfo.isUntrack;

        public bool IsInjectSkipCnSite() => coreInfo.isInjectSkipCNSite;

        public bool IsInjectGlobalImport() => coreInfo.isInjectImport;

        public string GetTitle()
        {
            var ci = coreInfo;
            var result = $"{ci.index}.[{ci.name}] {ci.summary}";
            return Misc.Utils.CutStr(result, 60);
        }

        public VgcApis.Models.Datas.CoreInfo GetAllInfo() => coreInfo;

        public string GetName() => coreInfo.name;
        public void SetName(string value)
        {
            coreInfo.name = value;
        }

        int statPort = -1;
        public int GetStatPort() => statPort;

        /// <summary>
        /// less or eq 0 means unavailable
        /// </summary>
        public void SetStatPort(int port) => statPort = port;

        string status = "";
        public string GetStatus() => status;
        public void SetStatus(string value) =>
            SetPropertyOnDemand(ref status, value);

        long speedTestResult = -1;
        public long GetSpeedTestResult() => speedTestResult;
        public void SetSpeedTestResult(long value) =>
            speedTestResult = value;

        public string GetRawUid() => coreInfo.uid;
        #endregion

        #region private methods
        void SetSettingsPropertyOnDemand(ref bool property, bool value, bool requireRestart = false)
        {
            if (property == value)
            {
                return;
            }

            property = value;

            // refresh UI immediately
            GetParent().InvokeEventOnPropertyChange();

            // time consuming things
            if (requireRestart && coreCtrl.IsCoreRunning())
            {
                coreCtrl.RestartCoreThen();
            }
        }

        bool SetPropertyOnDemand(ref string property, string value) =>
          SetPropertyOnDemandWorker(ref property, value);

        bool SetPropertyOnDemand<T>(ref T property, T value)
            where T : struct =>
            SetPropertyOnDemandWorker(ref property, value);

        bool SetPropertyOnDemandWorker<T>(ref T property, T value)
        {
            bool changed = false;
            if (!EqualityComparer<T>.Default.Equals(property, value))
            {
                property = value;
                GetParent().InvokeEventOnPropertyChange();
                changed = true;
            }
            return changed;
        }
        #endregion
    }

}
