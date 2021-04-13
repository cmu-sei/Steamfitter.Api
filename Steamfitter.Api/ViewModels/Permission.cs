// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Steamfitter.Api.ViewModels
{
    public class Permission : Base
    {
        public Guid Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
        
        public string Description { get; set; }

        public bool ReadOnly { get; set; }
    }
}

