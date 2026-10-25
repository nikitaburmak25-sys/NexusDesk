using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NexusDesk.Core.Services
{
    public class DiskScanner
    {
        public Task<List<DriveInfo>> GetDrivesAsync()
        {
            var drives = new List<DriveInfo>(DriveInfo.GetDrives());
            return Task.FromResult(drives);
        }
    }
}
