// See https://aka.ms/new-console-template for more information

using System.Threading;
using ReCompressMod;
using ZipModUtilities.Data;

ModConfig config = ModConfig.Create(SharedConsts.ConfigName);


ModChecker checker = new(config.ThreadCount);

checker.CheckModInDirectory(SharedConsts.TargetPath);


while (checker.ModNeedRecompressed > checker.ModRecompressedFinished )
{
    Thread.Sleep(10000);
}