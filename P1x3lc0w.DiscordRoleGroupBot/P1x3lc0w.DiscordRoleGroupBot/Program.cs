﻿using System;
using System.Threading;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    class Program
    {
        static void Main(string[] args)
        {
            _ = new Bot().StartBot(args[0]);
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
