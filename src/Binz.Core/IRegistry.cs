using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    public class RegistryConfig
    {
        public string? Address { get; set; } = null;

        public int HealthCheckIntervalSec { get; set; } = 10;

        public int HealthCheckTimeoutSec { get; set; } = 5;
    }


    public interface IRegistry<TRegisterInfo> : IAsyncDisposable where TRegisterInfo : RegistryInfo
    {
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registryInfo"></param>
        /// <returns></returns>
        public Task RegisterAsync(TRegisterInfo registryInfo);

        /// <summary>
        /// 取消注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registryInfo"></param>
        /// <returns></returns>
        public Task UnRegisterAsync(TRegisterInfo registryInfo);

        /// <summary>
        /// 取消注册所有服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceInfo"></param>
        /// <returns></returns>
        public Task UnRegisterAllAsync();

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registryInfo"></param>
        /// <returns></returns>
        public Task<List<ServiceInfo>> GetServiceAsync(TRegisterInfo registryInfo);

        /// <summary>
        /// 监听服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registryInfo"></param>
        /// <param name="OnServiceCahnged"></param>
        /// <returns></returns>
        public Task Watch(TRegisterInfo registryInfo, Func<List<ServiceInfo>, Task> OnServiceCahnged);
    }


    public interface IRegistry : IRegistry<RegistryInfo>
    {

    }
}
