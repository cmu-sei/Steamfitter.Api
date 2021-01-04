// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Steamfitter.Api.Infrastructure.Options
{
    public class ClientOptions
    {
        public ApiUrlSettings urls { get; set; }
    }

    public class ApiUrlSettings
    {
        public string playerApi { get; set; }
        public string vmApi { get; set; }
    }
}
