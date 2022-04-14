using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    public class RegistryConfig
    {
        public string Address { get; set; } = "http://localhost:8500/";

        public int HealthCheckIntervalSec { get; set; } = 10;

        public int HealthCheckTimeoutSec { get; set; } = 5;
    }


    public interface IRegistry<TRegisterInfo> : IDisposable where TRegisterInfo : RegisterInfo
    {
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceInfo"></param>
        /// <returns></returns>
        public Task RegisterAsync(TRegisterInfo serviceInfo);

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceInfo"></param>
        /// <returns></returns>
        public Task UnRegisterAsync(TRegisterInfo serviceInfo);

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceInfo"></param>
        /// <returns></returns>
        public Task UnRegisterAllAsync();

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceInfo"></param>
        /// <returns></returns>
        public Task<List<ServiceInfo>> GetServiceAsync(TRegisterInfo serviceInfo);

        /// <summary>
        /// 监听服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceInfo"></param>
        /// <param name="OnServiceCahnged"></param>
        /// <returns></returns>
        public Task Watch(TRegisterInfo serviceInfo, Func<List<ServiceInfo>, Task> OnServiceCahnged);
    }


    public interface IRegistry : IRegistry<RegisterInfo>
    {

    }
}
