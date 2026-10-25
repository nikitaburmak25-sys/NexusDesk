using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NexusDesk.Core.Services
{
    public class DiskScanner
    {
        public async Task<List<DriveInfo>> GetDrivesAsync()
        {
            return await Task.Run(() => DriveInfo.GetDrives().ToList());
        }
    }
}
