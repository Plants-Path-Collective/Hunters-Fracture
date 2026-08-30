# Items System


| Script | Tipo | Función |
|-----------|---|---|
| UnitInventory.cs | `MonoBehaviour` | Posee un Dictionary (`inventory`), el cual almacena todos los items del juego, al añadir uno con el metodo `AddItem()`, se busca el Item en la terminal, y se suma +1 (`inventory[itemID].quantity += quantity`) |
| Item.cs |  |  |