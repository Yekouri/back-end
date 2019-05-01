﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PolloPollo.Web.Security
{
    public static class HttpRequestExtensions
    {
        public static bool IsLocal(this HttpRequest req)
        {
            var connection = req.HttpContext.Connection;
            if (connection.RemoteIpAddress != null)
            {
                if (connection.LocalIpAddress != null)
                {
                    return (connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                        || req.Host.Value.StartsWith("localhost:"))
                        && (connection.LocalPort == 4001 || connection.LocalPort == 4000);
                }
                else
                {
                    return IPAddress.IsLoopback(connection.RemoteIpAddress)
                        && (connection.LocalPort == 4001 || connection.LocalPort == 4000);
                }
            }

            // for in memory TestServer or when dealing with default connection info
            if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
            {
                return true;
            }

            return false;
        }
    }
}
