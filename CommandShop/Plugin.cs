using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConfigStore;
using CurrencyBank;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Commands = TShockAPI.Commands;

namespace CommandShop
{
  [ApiVersion(2, 1)]
  public class Plugin : TerrariaPlugin
  {
    public override string Name => "CommandShop";
    public override string Author => "Newy";
    public override string Description => "Allows purchase of commands with CurrencyBank.";
    public override Version Version => typeof(Plugin).Assembly.GetName().Version;

    public Plugin(Main game) : base(game)
    {
    }

    public override void Initialize()
    {
      Config.Current = JsonConfig.Read<Config>(Path.Combine(TShock.SavePath, "CommandShop.json"));

      ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
    }

    protected override void Dispose(bool disposing)
    {
      Config.Current = null;

      ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);

      base.Dispose(disposing);
    }

    private static void OnPostInitialize(EventArgs args)
    {
      Commands.ChatCommands.Add(
        new Command("commandshop.use", PurchaseCommand, "cmdshop")
        {
          HelpText = "Allows purchase of commands with CurrencyBank.",
          HelpDesc = HelpText
        }
      );
    }

    private static readonly string[] HelpText =
    {
      "/cmdshop help: Show this message",
      "/cmdshop list: List available items",
      "/cmdshop buy <item>: Buy command"
    };

    private static async void PurchaseCommand(CommandArgs args)
    {
      if (args.Parameters.Count < 1)
      {
        args.Player.SendErrorMessage("Invalid usage! Usage:");

        foreach (var s in HelpText)
          args.Player.SendInfoMessage(s);

        return;
      }

      if (args.Parameters[0].Equals("help", StringComparison.OrdinalIgnoreCase))
      {
        foreach (var s in HelpText)
          args.Player.SendInfoMessage(s);

        return;
      }

      if (args.Parameters[0].Equals("list", StringComparison.OrdinalIgnoreCase))
      {
        args.Player.SendInfoMessage("Available items:");

        foreach (var i in Config.Current.Items)
          args.Player.SendInfoMessage("{0}: {1}", i.Name, i.Price);

        return;
      }

      args.Parameters.RemoveAt(0);

      var query = string.Join(" ", args.Parameters);

      if (string.IsNullOrWhiteSpace(query))
      {
        args.Player.SendErrorMessage("Invalid item name!");
        return;
      }

      var items = Config.Current.Items.Where(
        c => c.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)).ToArray();

      if (items.Length < 1)
      {
        args.Player.SendErrorMessage("Item \"{0}\" not found!", query);
        return;
      }

      if (items.Length > 1)
      {
        TShock.Utils.SendMultipleMatchError(args.Player, items.Select(t => t.Name));
        return;
      }

      var item = items[0];

      if (!args.Player.HasPermission(item.PurchasePermission))
      {
        args.Player.SendErrorMessage("You don't have permission to buy this item!");
        return;
      }

      var account = await BankMain.Bank.GetAsync(args.Player.User.Name);

      if (account == null)
      {
        args.Player.SendErrorMessage("You don't have a bank account!");
        return;
      }

      if (item.Price > account.Balance)
      {
        args.Player.SendErrorMessage("You cannot afford this item!");
        return;
      }

      await BankMain.Bank.ChangeByAsync(args.Player.User.Name, account.Balance - item.Price);

      ExecuteCommands(item, args.Player);
      args.Player.SendSuccessMessage("Purchased command \"{0}\" for {1}.", item.Name, item.Price);

      BankMain.Log.Write("{0} purchased command {1} for {2}",
        args.Player.User.Name ?? args.Player.Name, item.Name, item.Price);
    }

    private static void ExecuteCommands(CommandItem item, TSPlayer player)
    {
      foreach (var cmd in item.CommandsToExecute)
      {
        var fullcmd = new StringBuilder(cmd)
          .Replace("${player}", player.Name)
          .Replace("${user}", player.User?.Name ?? "")
          .Replace("${group}", player.User?.Group ?? "Unregistered")
          .Replace("${item}", item.Name)
          .Replace("${x}", player.TileX.ToString())
          .Replace("${y}", player.TileY.ToString())
          .Replace("${wx}", player.TPlayer.position.X.ToString(CultureInfo.InvariantCulture))
          .Replace("${wy}", player.TPlayer.position.Y.ToString(CultureInfo.InvariantCulture))
          .Replace("${life}", player.TPlayer.statLife.ToString())
          .Replace("${mana}", player.TPlayer.statMana.ToString())
          .Replace("${lifeMax}", player.TPlayer.statLife.ToString())
          .Replace("${manaMax}", player.TPlayer.statMana.ToString())
          .ToString();


        if (!Commands.HandleCommand(TSPlayer.Server, fullcmd))
        {
          player.SendErrorMessage(
            "There has been an error purchasing {0}. Please check with the server admins.", item.Name
          );
        }
      }
    }
  }
}