# CommandShop
Allows purchase of command usage with [CurrencyBank] currencies.
Requires [CurrencyBank] by Enerdy.

[CurrencyBank]: https://github.com/Enerdy/CurrencyBank

## Usage

`/cmdshop help`: Shows a help message.

`/cmdshop list`: Lists available items for purchase.

`/cmdshop buy <item>`: Purchase `<item>`.

## Configuration

A default configuration file is generated at
`tshock/CommandShop.json`.

```json
{
  "Items": [
    {
      "Name": "Sample",
      "PurchasePermission": "commandshop.buy.something",
      "Price": 200,
      "CommandsToExecute": [
        ".bc ${player} bought ${item}!",
        ".heal"
      ]
    }
  ]
}
```

### Available command variables

+ `${player}` = Buyer's name 
+ `${user}` = Buyer's user account name
+ `${group}` = Buyer's user group name
+ `${item}` = Bought item name
+ `${x}, ${y}` = Coordinates of buyer
+ `${wx}, ${wy}` = World coordinates of buyer (coordinates × 16)
+ `${life}, ${lifeMax}, ${mana}, ${manaMax}` = Stats of buyer