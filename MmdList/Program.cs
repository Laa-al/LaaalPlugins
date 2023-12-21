// See https://aka.ms/new-console-template for more information


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MmdList;

//const string vmdPath = @"D:\VMD Files";
const string vmdPath = @"D:\BaiduNetdiskWorkspace\Resources\VMD Files\";
const string fileName = "sorted.txt";

//MotionAnalyser.SimplyFileNames(vmdPath);
//List<MotionAnalyser> analysers = new();
//analysers = analysers.OrderBy(u=>u.DanceName).ToList();

MotionAnalyser root = MotionAnalyser.CreateFromPath(vmdPath, vmdPath);


foreach (MotionAnalyser sub in root.SubAnalysers)
{
    using FileStream fileStream = File.OpenWrite(Path.Combine(vmdPath, sub.BasePath + ".txt"));
    using StreamWriter writer = new(fileStream);
    foreach (MotionAnalyser analyser in sub.SubAnalysers)
    {
        analyser.WriteList(writer);
    }
}

return;

// void Write(MotionAnalyser analyser)
// {
//     if (analyser.IsMmdFolder())
//     {
//         analysers.Add(analyser);
//     }
// }