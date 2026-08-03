using System;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Util;

namespace Stratum.Desktop.Services
{
    public class CustomIconDecoder : ICustomIconDecoder
    {
        public Task<CustomIcon> DecodeAsync(byte[] data, bool shouldPreProcess)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Icon data is empty");
            }

            var id = HashUtil.Sha1(Convert.ToBase64String(data));
            return Task.FromResult(new CustomIcon { Id = id, Data = data });
        }
    }
}
