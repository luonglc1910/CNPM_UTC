#r "Microsoft.EntityFrameworkCore"
#r "bin/Debug/net9.0/HotelManagement.Web.dll"

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Web.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath("/Users/mac/Desktop/Code/UTC/CNPM_UTC")
    .AddJsonFile("appsettings.json")
    .Build();

var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();
optionsBuilder.UseSqlServer(configuration.GetConnectionString("HotelDb"));

using var db = new HotelDbContext(optionsBuilder.Options);
var rooms = db.Rooms.Select(r => new { r.RoomNumber, r.Status }).ToList();
foreach(var r in rooms) {
    Console.WriteLine($"{r.RoomNumber}: {r.Status}");
}
