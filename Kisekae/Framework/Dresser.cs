﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using xTile.Display;
using xTile.Layers;
using xTile.Tiles;

namespace Kisekae.Framework {
    class Dresser {
        /*********
        ** Pbulic Properties
        *********/
        /// <summary>Whether to move the dresser for compatibility with another mod that adds a stove in the same spot.</summary>
        public bool m_stoveInCorner { get; set; } = false;
        /// <summary>Whether to show the dresser.</summary>
        public bool m_isVisible { get; set; } = true;
        /*********
        ** Private Properties
        *********/
        /// <summary>Global Mod Interface.</summary>
        private IMod m_env;
        /// <summary>Original farm house level, if level up we need to patch dresser again.</summary>
        private int FarmHouseLevel = 0;
        /// <summary>Whether this is the first day since the player loaded their save.</summary>
        private bool m_isFirstDay = true;
        /// <summary>The name for new dresser tile sheet.</summary>
        private const string s_tilesheetName = "z_dresser";
        /// <summary>Adding a helper </summary>
        private IModHelper m_helper;

        /*********
        ** Public methods
        *********/
        public Dresser(IMod env) {
            m_env = env;
            m_helper = env.Helper;
        }

        public void init() {
            this.m_helper.Events.GameLoop.DayStarted += this.Events_AfterDayStarted;
            this.m_helper.Events.GameLoop.SaveLoaded += this.Events_AfterLoad;
            this.m_helper.Events.GameLoop.ReturnedToTitle += this.Events_AfterReturnToTitle;



        }

        /// <summary>Check if the dresser is in the location.</summary>
        /// <param name="pos">The tile location.</param>
        public bool IsDresser(Vector2 pos) {
            xTile.Tiles.Tile tile = Game1.currentLocation.map.GetLayer("Buildings").PickTile(new xTile.Dimensions.Location((int)pos.X * Game1.tileSize, (int)pos.Y * Game1.tileSize), Game1.viewport.Size);
            xTile.ObjectModel.PropertyValue propertyValue = null;
            tile?.Properties.TryGetValue("Action", out propertyValue);
            if (propertyValue?.ToString() != "Kisekae") {
                return false;
            }
            return true;
        }

        /// <summary>The event handler called when the mouse state changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Events_AfterDayStarted(object sender, EventArgs e) {
            FarmHouse farmhouse = (FarmHouse)Game1.getLocationFromName("FarmHouse");
            if (m_isFirstDay || farmhouse.upgradeLevel != this.FarmHouseLevel) {
                this.FarmHouseLevel = farmhouse.upgradeLevel;
                this.PatchFarmhouseTilesheet(farmhouse);
                this.PatchFarmhouseMap(farmhouse);
                m_isFirstDay = false;
            }
        }

        /// <summary>The event handler called when the player stops a session and returns to the title screen..</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Events_AfterReturnToTitle(object sender, EventArgs e) {
            m_isFirstDay = true;
        }

        /// <summary>The event handler called when the player loads a save and the world is ready.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Events_AfterLoad(object sender, EventArgs e) {
            //PatchFarmhouseTilesheet((FarmHouse)Game1.getLocationFromName("FarmHouse"));
        }

        /// <summary>Patch the dresser into the farmhouse tilesheet.</summary>
        /// <param name="farmhouse">The farmhouse to patch.</param>
        private void PatchFarmhouseTilesheet(FarmHouse farmhouse) {
            //m_env.Monitor.Log("PatchFarmhouseTilesheet");

            if (farmhouse.Map == null) {
                return;
            }
            string tilesheetPath = m_env.Helper.Content.GetActualAssetKey("overrides/dresser.png", ContentSource.ModFolder);

            // Add the tilesheet.
            TileSheet tilesheet = new TileSheet(
               id: s_tilesheetName,
               map: farmhouse.map,
               imageSource: tilesheetPath,
               sheetSize: new xTile.Dimensions.Size(1, 2),
               tileSize: new xTile.Dimensions.Size(16, 16)
            );
            farmhouse.map.AddTileSheet(tilesheet);
            farmhouse.map.LoadTileSheets(Game1.mapDisplayDevice);

            /*
            IReflectedField<Dictionary<TileSheet, Texture2D>> tilesheetTextures = m_env.Helper.Reflection.GetField<Dictionary<TileSheet, Texture2D>>(Game1.mapDisplayDevice as XnaDisplayDevice, "m_tileSheetTextures");
            Texture2D texture = null;
            if (farmhouse.map == null) {
                return;
            }
            TileSheet t = farmhouse.map.GetTileSheet("untitled tile sheet");
            tilesheetTextures.GetValue().TryGetValue(t, out texture);
            if (texture != null) {
                //System.IO.Stream f = new System.IO.FileStream("XXXAAA.png", System.IO.FileMode.CreateNew);
                //texture.SaveAsPng(f, texture.Width, texture.Height);
                m_contentHelper.PatchTexture(ref texture, "dresser.png", 0, 231, 16, 16);
                m_contentHelper.PatchTexture(ref texture, "dresser.png", 1, 232, 16, 16);
            }
            */
        }

        /// <summary>Patch the dresser into the farmhouse map.</summary>
        /// <param name="farmhouse">The farmhouse to patch.</param>
        private void PatchFarmhouseMap(FarmHouse farmhouse) {
            if (!m_isVisible) {
                return;
            }

            if (farmhouse.Map == null) {
                return;
            }

            // get dresser coordinates
            Point position;
            switch (farmhouse.upgradeLevel) {
                case 0:
                    position = new Point(m_stoveInCorner ? 7 : 10, 2);
                    break;
                case 1:
                    position = new Point(Game1.player.isMarried() ? 25 : 28, 2);
                    break;
                case 2:
                    position = new Point(33, 11);
                    break;
                case 3:
                    position = new Point(33, 11);
                    break;
                default:
                    m_env.Monitor.Log($"Couldn't patch dresser into farmhouse, unknown upgrade level {farmhouse.upgradeLevel}", LogLevel.Warn);
                    return;
            }

            // inject dresser
            Tile[] tiles = {
                new Tile(TileLayer.Front, position.X, position.Y, 0, s_tilesheetName), // dresser top
                new Tile(TileLayer.Buildings, position.X, position.Y + 1, 1, s_tilesheetName) // dresser bottom
            };
            foreach (Tile tile in tiles) {
                Layer layer = farmhouse.Map.GetLayer(tile.LayerName);
                TileSheet tilesheet = farmhouse.Map.GetTileSheet(tile.Tilesheet);

                if (layer.Tiles[tile.X, tile.Y] == null || layer.Tiles[tile.X, tile.Y].TileSheet.Id != tile.Tilesheet) {
                    /*
                    if (layer.Tiles[tile.X, tile.Y] == null) {
                        m_env.Monitor.Log("NULL:");
                    } else {
                        m_env.Monitor.Log("Not NULL:" + layer.Tiles[tile.X, tile.Y].TileSheet.Id + "|" + layer.Tiles[tile.X, tile.Y].TileSheet.ImageSource + "|" + layer.Tiles[tile.X, tile.Y].ToString());
                    }
                    */
                    layer.Tiles[tile.X, tile.Y] = new StaticTile(layer, tilesheet, BlendMode.Alpha, tile.TileID);
                    //m_env.Monitor.Log("changed to:" + layer.Tiles[tile.X, tile.Y].TileSheet.Id + "|" + layer.Tiles[tile.X, tile.Y].TileSheet.ImageSource + "|" + layer.Tiles[tile.X, tile.Y].ToString());
                } else {
                    farmhouse.setMapTileIndex(tile.X, tile.Y, tile.TileID, layer.Id);
                }
            }

            // add action attribute
            farmhouse.setTileProperty(position.X, position.Y + 1, "Buildings", "Action", "Kisekae");
        }
    }
}
