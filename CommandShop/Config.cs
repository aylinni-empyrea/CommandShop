using System.Collections.Generic;
using ConfigStore;

namespace CommandShop
{
  public class Config : JsonConfig
  {
    public static Config Current { get; internal set; }

    public List<CommandItem> Items { get; set; } = new List<CommandItem>
    {
      new CommandItem
      {
        Name = "Sample",
        Price = 200,
        PurchasePermission = "commandshop.buy.something",
        CommandsToExecute = new List<string>
        {
          ".bc ${player} bought ${item}!",
          ".heal"
        }
      }
    };
  }

  public class CommandItem
  {
    public string Name { get; set; }
    public string PurchasePermission { get; set; } = "commandshop.buy";

    public long Price { get; set; }

    public List<string> CommandsToExecute { get; set; }
  }
}