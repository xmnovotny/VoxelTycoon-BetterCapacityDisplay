using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using System.Collections.Generic;
using XMNUtils;

namespace BetterCapacityDisplay
{
    public class StorageViewHelper
    {
        private const int MAX_ITEMS_COUNT = 3;
        private readonly int[] TITLE_TEXT_LENGTHS_LIMITS = { 20, 15 };

        private int _limitedItemsCount { get 
            {
                var length = _vehicle.Name.Length;
                for (int i=0; i<MAX_ITEMS_COUNT - 1; i++)
                {
                    if (TITLE_TEXT_LENGTHS_LIMITS[i] <= length)
                    {
                        return i + 1;
                    }
                }
                return MAX_ITEMS_COUNT ; 
            } 
        }

        private class StorageData
        {
            public Transform ItemsContainer { get; set; }
            public Transform CapacityValueContainer { get; set; }
            public Text CapacityValueText { get; set; }
        }

        private struct StorageItemInfo
        {
            public int Count;
            public int Capacity;
        }

        private readonly StorageData[] _storageData = new StorageData[MAX_ITEMS_COUNT];
        private Transform _origItemsContainer;
        private Transform _capacityOverflowTextContainer;
        private Text _capacityOverflowText;
        public int? _version;

        private Vehicle _vehicle;
        private static DepotWindowVehicleListItemStoragesView _storagesViewTemplate;

        public void ClearItems(bool inactivate = false)
        {
            foreach (StorageData data in _storageData)
            {
                data.ItemsContainer.Clear();
                if (inactivate)
                {
                    data.ItemsContainer.gameObject.SetActive(false);
                    data.CapacityValueContainer.gameObject.SetActive(false);
                }
            }
        }

        private List<Storage> GetVehicleStorages()
        {
            return (from x in this._vehicle.Units.ToList()
                                  select x.Storage into x
                                  where x != null
                                  select x).ToList<Storage>();
        }
        private List<Item> GetVehicleItems(List<Storage> storageList)
        {
            return (from x in storageList
                    select x.Item into x
                    where x != null
                    select x).Distinct<Item>().ToList<Item>();
        }

        private StorageItemInfo GetItemSum(List<Storage> storageList, Item item)
        {
            StorageItemInfo result;
            result.Count = (from x in storageList
                            where x.Item == item
                            select x).Sum((Storage x) => x.Count);
            result.Capacity = (from x in storageList
                               where x.Item == item
                               select x).Sum((Storage x) => x.Capacity);
            return result;
        }

        private string[] GetCapacityTexts(List<Storage> storageList, List<Item> itemList, int count)
        {
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                var info = GetItemSum(storageList, itemList[i]);
                result[i] = string.Format("{0}/{1}", info.Count, info.Capacity);
            }
            return result;
        }

        public void Invalidate(int? origVersion)
        {
            if (!Helper.Set<int?>(ref _version, origVersion))
            {
                return;
            }
            ClearItems();

            _origItemsContainer.gameObject.SetActive(false);

            List<Storage> storageList = GetVehicleStorages();
            List<Item> itemList = GetVehicleItems(storageList);
            int count = Math.Min(_limitedItemsCount, itemList.Count);

            string[] capacityTexts = null;

            if (count > 1)
            {
                capacityTexts = GetCapacityTexts(storageList, itemList, count);
            }

            for (int i = 0; i < count; i++)
            {
                UnityEngine.Object.Instantiate<Image>(R.Game.UI.DepotWindow.DepotWindowVehicleListItemStoragesViewItem, this._storageData[i].ItemsContainer).sprite = LazyManager<IconRenderer>.Current.GetItemIcon(itemList[i].AssetId);
                _storageData[i].ItemsContainer.gameObject.SetActive(true);
                _storageData[i].CapacityValueContainer.gameObject.SetActive(true);
                if (count > 1)
                {
                    _storageData[i].CapacityValueText.text = capacityTexts[i];
                }
            }
            for (int i = count; i < MAX_ITEMS_COUNT; i++)
            {
                _storageData[i].ItemsContainer.gameObject.SetActive(false);
                _storageData[i].CapacityValueContainer.gameObject.SetActive(false);
            }
            if (count<itemList.Count)
            {
                _capacityOverflowTextContainer.SetActive(true);
                _capacityOverflowText.text = string.Format("+{0}", itemList.Count - count);
            } else
            {
                _capacityOverflowTextContainer.SetActive(false);
            }
        }

        public StorageViewHelper(DepotWindowVehicleListItemStoragesView storagesView, Vehicle vehicle): base()
        {
            this._vehicle = vehicle;
            _origItemsContainer = storagesView.transform.Find("ItemsContainer");
            _origItemsContainer.Clear(true);

            var origCapacityValueContainer = storagesView.transform.Find("CapacityValueContainer");
            _capacityOverflowTextContainer = storagesView.transform.Find("OverflowCountTextContainer");
            _capacityOverflowText = _capacityOverflowTextContainer.GetComponentInChildren<Text>();

            Transform capacityValueContainer = origCapacityValueContainer;
            Transform itemsContainer;

            for (var i = 0; i < MAX_ITEMS_COUNT; i++)
            {
                FileLog.Log(i.ToString());
                itemsContainer = UnityEngine.Object.Instantiate<RectTransform>((RectTransform)_origItemsContainer, storagesView.transform);
                itemsContainer.SetSiblingIndex(i*2);
                itemsContainer.name = _origItemsContainer.name + (i + 1);

                if (i>0)
                {
                    capacityValueContainer = UnityEngine.Object.Instantiate<RectTransform>((RectTransform)origCapacityValueContainer, storagesView.transform);
                    capacityValueContainer.name = origCapacityValueContainer.name + (i + 1);
                }
                capacityValueContainer.SetSiblingIndex(i * 2 + 1);

                _storageData[i] = new StorageData
                {
                    ItemsContainer = itemsContainer,
                    CapacityValueContainer = capacityValueContainer,
                    CapacityValueText = capacityValueContainer.GetComponentInChildren<Text>()
                };
            }

            ClearItems();
        }

        public static DepotWindowVehicleListItemStoragesView InstantinateStoragesViewForVehicleWindow(Transform transform, Vehicle vehicle)
        {
            DepotWindowVehicleListItemStoragesView result = InstantinateStoragesView(transform, vehicle);
            var render = result.gameObject.GetComponent<CanvasRenderer>();
            if (render)
            {
                render.SetAlpha(0);
                foreach (Text text in result.GetComponentsInChildren<Text>(true))
                {
                    text.color = Color.white;
                }
            }

            return result;
        }

        private static DepotWindowVehicleListItemStoragesView InstantinateStoragesView(Transform transform, Vehicle vehicle)
        {
            if (_storagesViewTemplate == null)
            {
                var listItem = UnityEngine.Object.Instantiate<DepotWindowVehicleListItem>(R.Game.UI.DepotWindow.DepotWindowVehicleListItem);
                _storagesViewTemplate = listItem.StoragesView;
                _storagesViewTemplate.transform.SetParent(null);
                _storagesViewTemplate.gameObject.SetActive(false);
                listItem.DestroyGameObject();
            }

            var result = UnityEngine.Object.Instantiate<DepotWindowVehicleListItemStoragesView>(_storagesViewTemplate, transform);
            result.Initialize(vehicle);
            return result;
        }
    }
}
