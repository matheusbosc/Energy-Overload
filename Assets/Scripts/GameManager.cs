using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace com.matheusbosc.energyoverload
{
    public class GameManager : MonoBehaviour
    {
        #region Variables
        
        [Header("Game Items")]
        public BuildingItem[] buildingItems;
        public BuildingInfo[] possibleBuildings;
        public Toggle[] toggles;
        public int startingLives = 3;

        [Header("Round Settings")] 
        public float roundPointMultiplier = 1.05f;
        public float roundMoneyMultiplier = 1.1f;
        public float roundSpeedMultiplier = 1.05f;
        public float maxTime = 60f, currentTime;

        [Header("Currency")]
        public float money, startingMoney, moneyGained;
        public int points;
        
        [Header("Energy")]
        public float energyLimit = 535;
        public TextMeshProUGUI maxEnergyText, currentEnergyText, timeText;
        public float energyUpdateDelay = 2f, overloadBuffer = 5f;

        [Header("Shop & Upgrades")]
        public int pointMultiplier = 1;
        public float bufferIncrease = 0;
        public float maxEnergyIncrease = 0;
        public float timeDecrease = 0;
        public ShopItem[] shopItems;
        public int currentlySelectedShopItem = 0;
        public TextMeshProUGUI shopNameText, shopDescriptionText, shopPriceText, currentBalanceShopText;
        public Button purchaseButton;

        [Header("UI")]
        public GameObject[] panels;
        public TextMeshProUGUI winLoseText, moneyGainedText, pointsGainedText, totalBalanceText, timeLeftText, moneyGainedInGameText, pointsGainedInGameText, lostReasonText;
        public Button loseWinContinueButton;
        public Color normalTextColor, overloadedColor, disabledColor;

        [Header("Other")] 
        public AudioSource mainTheme, gameTheme, slowTheme, pauseSfx;
        public Texture2D cursor;
        
        // ------- Private Variables ------- //
        private int currentPanel = 3, menuBeforePause;
        private int currentLives;
        private bool isUpdating = false, isTiming = false, isGameOver = false, youWin = false, isPaused = false, hasBegun = false, settingsToPause = false;
        private int currentRound = 0;
        private float currentEnergyUsage;
        private AudioSettings audioSettings;
        [SerializeField] private List<Button> disabledButtons;
        
        #endregion
        
        #region Unity Methods
        
        public void Start()
        {
            //BeginGame();
            audioSettings = gameObject.GetComponent<AudioSettings>();
            //Cursor.SetCursor(cursor, new Vector2(0,0), CursorMode.Auto);
        }

        private void Update()
        {
            
            if (hasBegun)
            {
                if (!isUpdating && !isGameOver && !youWin && !isPaused)
                {
                    StartCoroutine(UpdateEnergy());
                }
                if (Input.GetKeyDown(KeyCode.Escape)){TogglePause();}
                currentEnergyText.text = currentEnergyUsage.ToString("0.00");
                if (currentEnergyUsage > energyLimit && !isGameOver && !youWin && !isPaused)
                {
                    // U LOSE!!!
                    print("You Lose due to grid overload.");
                    LoseGame(3);
                }
            
                /* ---------------------- SHOP ----------------------- */

                currentBalanceShopText.text = money.ToString("0.00");
            }
            
        }

        private void FixedUpdate()
        {
            if (!isTiming && !isGameOver && !youWin && !isPaused && hasBegun)
            {
                StartCoroutine(CountDown());
            }
        }
        
        #endregion

        #region UI

        public void BeginGame()
        {
            hasBegun = true;
            Spawn();
            currentEnergyUsage = 0;
            currentlySelectedShopItem = 0;
            SwitchMenu(0);
            foreach (var toggle in toggles)
            {
                toggle.isOn = true;
                toggle.gameObject.GetComponent<Animator>().SetTrigger("TurnOn");
            }

            energyLimit = 0;
            foreach (var item in buildingItems)
            {
                BuildingInfo __info = item.info;
                TextMeshProUGUI __name = item.buildingName;
                TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                TextMeshProUGUI __max = item.maxEnergy;
                int __id = item.buildingId;

                __energy.text = (__info.maxPowerUsage / 2).ToString("0.00");
                __name.text = __info.buildingName;
                __max.text = __info.maxPowerUsage.ToString("0.00");
                item.icon.sprite = __info.icon;
                currentEnergyUsage += __info.maxPowerUsage / 2;
                item.isDisabled = false;
                energyLimit += __info.maxPowerUsage;
            }

            points = 0;
            money = 20;
            moneyGained = 0;
            currentLives = startingLives;
            startingMoney = money;
            currentTime = maxTime;
            maxEnergyText.text = energyLimit.ToString("0.00");
            currentEnergyText.text = currentEnergyUsage.ToString("0.00");
            currentRound = 0;
            isUpdating = false;
            isTiming = false; 
            isGameOver = false;
            youWin = false;
            
            int maxItemID = 0;

            foreach (var item in shopItems)
            {
                maxItemID += 1;
            }
            
            if (currentlySelectedShopItem == 0 || currentlySelectedShopItem > maxItemID)
            {
                shopNameText.text = "";
                shopDescriptionText.text = "";
                shopPriceText.text = "";
                purchaseButton.interactable = false;
            }
            else
            {
                var currentItem = shopItems[currentlySelectedShopItem];
                shopNameText.text = currentItem.name;
                shopDescriptionText.text = currentItem.description;
                shopPriceText.text = "$" + currentItem.price.ToString("0.00");
                purchaseButton.interactable = true;
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(() => BuyItem(currentlySelectedShopItem));
            }
            
            float moneyAmountAdded = 0;
            
            currentEnergyUsage = 0;
            foreach (var item in buildingItems)
            {
                BuildingInfo __info = item.info;
                TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                if (!item.isDisabled)
                {
                    float curEnergy = NewEnergy(__info.maxPowerUsage + maxEnergyIncrease, __energy.text);
                    __energy.text = curEnergy.ToString("0.00");
                    currentEnergyUsage += curEnergy;
                    item.buildingName.color = normalTextColor;
                    item.maxEnergy.color = normalTextColor;
                    item.icon.color = normalTextColor;

                    if (curEnergy >= __info.maxPowerUsage + maxEnergyIncrease)
                    {
                        __energy.color = overloadedColor;
                        item.overloadingText.color = overloadedColor;
                        moneyAmountAdded += 0.2f * (1 + (currentRound * roundMoneyMultiplier));
                        points += (1 * Mathf.RoundToInt((currentRound * roundPointMultiplier) + 1)) * pointMultiplier;
                    }
                    else
                    {
                        __energy.color = normalTextColor;
                        item.overloadingText.color = disabledColor;
                        moneyAmountAdded += 0.5f * (1 + (currentRound * roundMoneyMultiplier));
                        points += (3 * Mathf.RoundToInt((currentRound * roundPointMultiplier) + 1)) * pointMultiplier;
                    }

                    if (curEnergy >= (__info.maxPowerUsage + maxEnergyIncrease + overloadBuffer + bufferIncrease))
                    {
                        // U LOSE!!!
                        print("You Lose due to building overload.");
                        LoseGame(1);
                    }

                    if (curEnergy <= 0)
                    {
                        print("You Lose due to building underload.");

                        LoseGame(2);
                    }
                }
                else
                {
                    float curEnergy = float.Parse(__energy.text) - 3f;
                    __energy.text = curEnergy.ToString("0.00");
                    currentEnergyUsage += curEnergy;
                    
                    __energy.color = disabledColor;
                    item.buildingName.color = disabledColor;
                    item.maxEnergy.color = disabledColor;
                    item.overloadingText.color = disabledColor;
                    item.icon.color = disabledColor;

                    moneyAmountAdded -= 0.4f * (1 + (currentRound * roundMoneyMultiplier));
                    if (curEnergy <= 0)
                    {
                        print("You Lose due to building underload.");

                        LoseGame(2);
                    }

                }
            }

            moneyGained += moneyAmountAdded;
            print("Money: $" + moneyGained + " | Points: " + points);
            moneyGainedInGameText.text = moneyGained.ToString("0.00");
            pointsGainedInGameText.text = points.ToString();
        }

        public void EndGame()
        {
            hasBegun = false;
            isPaused = false;
        }
        
        public void TogglePause()
        {
            if (!isPaused)
            {
                isPaused = true;
                menuBeforePause = currentPanel;
                SwitchMenu(4);
                pauseSfx.Play();
                audioSettings.currentlyActiveAudio.Pause();
                
            }
            else
            {
                isPaused = false;
                SwitchMenu(menuBeforePause);
                pauseSfx.Play();
                audioSettings.currentlyActiveAudio.UnPause();
                
            }
        }

        public void SwitchMenu(int menuID)
        {
            panels[currentPanel].SetActive(false);
            panels[menuID].SetActive(true);
            currentPanel = menuID;
        }

        public void ToSettings(bool fromPause)
        {
            SwitchMenu(6);
            settingsToPause = fromPause;
        }
        
        public void BackFromSettings()
        {
            if (settingsToPause)
            {
                panels[6].SetActive(false);
                panels[4].SetActive(true);
                currentPanel = 4;
            }
            else
            {
                panels[6].SetActive(false);
                panels[3].SetActive(true);
                currentPanel = 3;
            }
        }

        public void Quit()
        {
            Application.Quit();
        }

        #endregion

        #region Shop

        public void SelectItem(int itemID)
        {
            currentlySelectedShopItem = itemID;
            
            int maxItemID = -1;

            foreach (var item in shopItems)
            {
                maxItemID += 1;
            }
            
            if (currentlySelectedShopItem == -1 || currentlySelectedShopItem > maxItemID)
            {
                shopNameText.text = "";
                shopDescriptionText.text = "";
                shopPriceText.text = "";
                purchaseButton.interactable = false;
            }
            else
            {
                var currentItem = shopItems[currentlySelectedShopItem];
                shopNameText.text = currentItem.name;
                shopDescriptionText.text = currentItem.description;
                shopPriceText.text = "$" + currentItem.price.ToString("0.00");
                if (money >= currentItem.price)
                {
                    purchaseButton.interactable = true;
                    purchaseButton.onClick.RemoveAllListeners();
                    purchaseButton.onClick.AddListener(() => BuyItem(currentlySelectedShopItem));
                }
            }
        }
        
        public void BuyItem(int itemID)
        {
            
            int maxItemID = -1;

            foreach (var item in shopItems)
            {
                maxItemID += 1;
            }

            if (currentlySelectedShopItem == -1 || currentlySelectedShopItem > maxItemID)
            {
                print("Nothing bought");
            }
            else
            {
                foreach (var item in shopItems)
                {
                    if (item.itemId == itemID)
                    {
                        money -= item.price;

                        if (item.itemType == shopItemType.ptsMult)
                        {
                            pointMultiplier = item.intModifier;
                        }
                        else if (item.itemType == shopItemType.buffInc)
                        {
                            bufferIncrease = item.floatModifier;
                        }
                        else if (item.itemType == shopItemType.nrgInc)
                        {
                            maxEnergyIncrease = item.floatModifier;
                        }
                        else
                        {
                            timeDecrease = item.floatModifier;
                        }

                        foreach (var button in item.buttonsInCategory)
                        {
                            button.interactable = false;
                            disabledButtons.Add(button);
                        }
                        
                        SelectItem(-1);
                    }
                }
            }
        }

        #endregion
        
        #region Game
        
        public void ResetGame()
        {
            Spawn();
            currentEnergyUsage = 0;
            currentlySelectedShopItem = 0;
            SwitchMenu(0);
            startingMoney = money;
            energyLimit = 0;
            foreach (var toggle in toggles)
            {
                toggle.isOn = true;
                toggle.gameObject.GetComponent<Animator>().ResetTrigger("Switch");
                toggle.gameObject.GetComponent<Animator>().SetTrigger("TurnOn");
            }
            foreach (var item in buildingItems)
            {
                BuildingInfo __info = item.info;
                TextMeshProUGUI __name = item.buildingName;
                TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                TextMeshProUGUI __max = item.maxEnergy;
                int __id = item.buildingId;

                __energy.text = (__info.maxPowerUsage / 2).ToString("0.00");
                __name.text = __info.buildingName;
                __max.text = __info.maxPowerUsage.ToString("0.00");
                item.icon.sprite = __info.icon;
                currentEnergyUsage += __info.maxPowerUsage / 2;
                item.isDisabled = false;
                energyLimit += __info.maxPowerUsage;
            }

            points = 0;
            moneyGained = 0;
            currentTime = maxTime - timeDecrease;
            maxEnergyText.text = energyLimit.ToString("0.00");
            currentEnergyText.text = currentEnergyUsage.ToString("0.00");
            currentRound += 1;
            isUpdating = false;
            isTiming = false; 
            isGameOver = false;
            youWin = false;
            
            float moneyAmountAdded = 0;
            
            currentEnergyUsage = 0;
            foreach (var item in buildingItems)
            {
                BuildingInfo __info = item.info;
                TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                if (!item.isDisabled)
                {
                    float curEnergy = NewEnergy(__info.maxPowerUsage + maxEnergyIncrease, __energy.text);
                    __energy.text = curEnergy.ToString("0.00");
                    currentEnergyUsage += curEnergy;
                    item.buildingName.color = normalTextColor;
                    item.maxEnergy.color = normalTextColor;
                    item.icon.color = normalTextColor;

                    if (curEnergy >= __info.maxPowerUsage + maxEnergyIncrease)
                    {
                        __energy.color = overloadedColor;
                        item.overloadingText.color = overloadedColor;
                        moneyAmountAdded += 0.2f * (1 + (currentRound * roundMoneyMultiplier));
                        points += (1 * Mathf.RoundToInt((currentRound * roundPointMultiplier) + 1)) * pointMultiplier;
                    }
                    else
                    {
                        __energy.color = normalTextColor;
                        item.overloadingText.color = disabledColor;
                        moneyAmountAdded += 0.5f * (1 + (currentRound * roundMoneyMultiplier));
                        points += (3 * Mathf.RoundToInt((currentRound * roundPointMultiplier) + 1)) * pointMultiplier;
                    }

                    if (curEnergy >= (__info.maxPowerUsage + maxEnergyIncrease + overloadBuffer + bufferIncrease))
                    {
                        // U LOSE!!!
                        print("You Lose due to building overload.");
                        LoseGame(1);
                    }

                    if (curEnergy <= 0)
                    {
                        print("You Lose due to building underload.");

                        LoseGame(2);
                    }
                }
                else
                {
                    float curEnergy = float.Parse(__energy.text) - 3f;
                    __energy.text = curEnergy.ToString("0.00");
                    currentEnergyUsage += curEnergy;
                    
                    __energy.color = disabledColor;
                    item.buildingName.color = disabledColor;
                    item.maxEnergy.color = disabledColor;
                    item.overloadingText.color = disabledColor;
                    item.icon.color = disabledColor;

                    moneyAmountAdded -= 0.4f * (1 + (currentRound * roundMoneyMultiplier));
                    if (curEnergy <= 0)
                    {
                        print("You Lose due to building underload.");

                        LoseGame(2);
                    }

                }
            }

            moneyGained += moneyAmountAdded;
            print("Money: $" + moneyGained + " | Points: " + points);
            moneyGainedInGameText.text = moneyGained.ToString("0.00");
            pointsGainedInGameText.text = points.ToString();
        }
        
        IEnumerator CountDown()
        {
            isTiming = true;
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
            timeText.text = currentTime.ToString("0");
            if (currentTime <= 0)
            {
                WinGame();
            }
            isTiming = false;
        }

        public void DisablePower(int buildingId) // Toggles power, not disable
        {
            if (!youWin && !isGameOver)
            {
                foreach (var item in buildingItems)
                {
                    if (item.buildingId == buildingId)
                    {
                        if (item.isDisabled)
                        {
                        
                            TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                            BuildingInfo __info = item.info;
                            item.isDisabled = false;
                            item.buildingName.color = normalTextColor;
                            item.maxEnergy.color = normalTextColor;
                            item.icon.color = normalTextColor;
                            if (float.Parse(__energy.text) >= __info.maxPowerUsage)
                            {
                                __energy.color = overloadedColor;
                                item.overloadingText.color = overloadedColor;
                            }
                            else
                            {
                                __energy.color = normalTextColor;
                                item.overloadingText.color = disabledColor;
                            }
                        }
                        else
                        {
                            TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                            item.isDisabled = true;
                            __energy.color = disabledColor;
                            item.buildingName.color = disabledColor;
                            item.maxEnergy.color = disabledColor;
                            item.overloadingText.color = disabledColor;
                            item.icon.color = disabledColor;
                        }
                    }
                }
            }
        }
        

        IEnumerator UpdateEnergy()
        {
            isUpdating = true;
            float speedDifference;
            if ((1 - (currentRound * roundSpeedMultiplier)) <= 0.35f)
            {
                speedDifference = 0.35f;
            }
            else
            {
                speedDifference = (1 - (currentRound * roundSpeedMultiplier));
            }
            print ("Speed difference: " + speedDifference.ToString("0.00"));
            print ("Speed: " + (energyUpdateDelay - speedDifference).ToString("0.00"));
            
            yield return new WaitForSeconds((energyUpdateDelay * speedDifference));

            float moneyAmountAdded = 0;
            
            currentEnergyUsage = 0;
            foreach (var item in buildingItems)
            {
                BuildingInfo __info = item.info;
                TextMeshProUGUI __energy = item.buildingEnergyConsumption;
                if (!item.isDisabled)
                {
                    float curEnergy = NewEnergy(__info.maxPowerUsage + maxEnergyIncrease, __energy.text);
                    __energy.text = curEnergy.ToString("0.00");
                    currentEnergyUsage += curEnergy;
                    item.buildingName.color = normalTextColor;
                    item.maxEnergy.color = normalTextColor;
                    item.icon.color = normalTextColor;

                    if (curEnergy >= __info.maxPowerUsage + maxEnergyIncrease)
                    {
                        __energy.color = overloadedColor;
                        item.overloadingText.color = overloadedColor;
                        moneyAmountAdded += 0.2f * (1 + (currentRound * roundMoneyMultiplier));
                        points += (1 * Mathf.RoundToInt((currentRound * roundPointMultiplier) + 1)) * pointMultiplier;
                    }
                    else
                    {
                        __energy.color = normalTextColor;
                        item.overloadingText.color = disabledColor;
                        moneyAmountAdded += 0.5f * (1 + (currentRound * roundMoneyMultiplier));
                        points += (3 * Mathf.RoundToInt((currentRound * roundPointMultiplier) + 1)) * pointMultiplier;
                    }

                    if (curEnergy >= (__info.maxPowerUsage + maxEnergyIncrease + overloadBuffer + bufferIncrease))
                    {
                        // U LOSE!!!
                        print("You Lose due to building overload.");
                        LoseGame(1);
                    }

                    if (curEnergy <= 0)
                    {
                        print("You Lose due to building underload.");

                        LoseGame(2);
                    }
                }
                else
                {
                    float curEnergy = float.Parse(__energy.text) - 3f;
                    __energy.text = curEnergy.ToString("0.00");
                    currentEnergyUsage += curEnergy;
                    
                    __energy.color = disabledColor;
                    item.buildingName.color = disabledColor;
                    item.maxEnergy.color = disabledColor;
                    item.overloadingText.color = disabledColor;
                    item.icon.color = disabledColor;

                    moneyAmountAdded -= 0.4f * (1 + (currentRound * roundMoneyMultiplier));
                    if (curEnergy <= 0)
                    {
                        print("You Lose due to building underload.");

                        LoseGame(2);
                    }

                }
            }

            moneyGained += moneyAmountAdded;
            print("Money: $" + moneyGained + " | Points: " + points);
            moneyGainedInGameText.text = moneyGained.ToString("0.00");
            pointsGainedInGameText.text = points.ToString();
            isUpdating = false;
        }

        private void LoseGame(int reason)
        {
            audioSettings.FadeIn(slowTheme);
            audioSettings.FadeOut(gameTheme);
            currentLives -= 1;
            if (currentLives <= 0)
            {
                
                points = 0;
                moneyGained = 0;
                // ---- Shop Settings ---- //
                pointMultiplier = 1;
                bufferIncrease = 0;
                maxEnergyIncrease = 0;
                timeDecrease = 0;
                currentlySelectedShopItem = 0;

                if (disabledButtons != null)
                {
                    foreach (var button in disabledButtons)
                    {
                        button.interactable = true;
                    }
                
                    disabledButtons.Clear();
                }
                
                // ---- UI ---- //
                SwitchMenu(1);
                winLoseText.text = "Game Over";
                moneyGainedText.text = "";
                pointsGainedText.text = "";
                totalBalanceText.text = "";
                lostReasonText.text = "FINAL BALANCE: $"+money.ToString("0.00");
                
                money = 0;
                
                loseWinContinueButton.onClick.RemoveAllListeners();
                loseWinContinueButton.onClick.AddListener(() => SwitchMenu(3));
                loseWinContinueButton.onClick.AddListener(() => audioSettings.FadeOut(slowTheme));
                loseWinContinueButton.onClick.AddListener(() => audioSettings.FadeIn(mainTheme));
                
                EndGame();
            }
            else
            {
                isGameOver = true;
            
                // ---- Currency ---- //
                points = 0;
                moneyGained = moneyGained / 4;
                money += moneyGained;
            
                // ---- Shop Settings ---- //
                pointMultiplier = 1;
                bufferIncrease = 0;
                maxEnergyIncrease = 0;
                timeDecrease = 0;
                currentlySelectedShopItem = 0;

                if (disabledButtons != null)
                {
                    foreach (var button in disabledButtons)
                    {
                        button.interactable = true;
                    }
                
                    disabledButtons.Clear();
                }
            
                // ---- UI ---- //
                SwitchMenu(1);
                winLoseText.text = "STRIKE " + (startingLives - currentLives).ToString() + " / " + startingLives;
                moneyGainedText.text = "GAINED $" + moneyGained.ToString("0.00");
                pointsGainedText.text = "NO POINTS";
                totalBalanceText.text = "BALANCE $" + money.ToString("0.00");

                if (reason == 1)
                {
                    lostReasonText.text = "REASON: BUILDING OVERLOAD";
                } else if (reason == 2)
                {
                    lostReasonText.text = "REASON: BUILDING UNDERLOAD";
                } else if (reason == 3)
                {
                    lostReasonText.text = "REASON: GRID OVERLOAD";
                }
                else
                {
                    lostReasonText.text = "REASON: SKILL ISSUE";
                }
                
                loseWinContinueButton.onClick.RemoveAllListeners();
                loseWinContinueButton.onClick.AddListener(() => SwitchMenu(2));
            }
        }

        private void WinGame()
        {
            audioSettings.FadeIn(slowTheme);
            audioSettings.FadeOut(gameTheme);
            youWin = true;
            
            // ---- Currency ---- //
            moneyGained += points * 0.1f;
            currentTime = 0;
            money += moneyGained;
            
            
            // ---- Shop Settings ---- //
            pointMultiplier = 1;
            bufferIncrease = 0;
            maxEnergyIncrease = 0;
            timeDecrease = 0;
            currentlySelectedShopItem = 0;
            
            if (disabledButtons != null)
            {
                foreach (var button in disabledButtons)
                {
                    button.interactable = true;
                }
                
                disabledButtons.Clear();
            }
            
            // ---- UI ---- //
            SwitchMenu(1);
            timeText.text = currentTime.ToString("0");
            winLoseText.text = "GOOD JOB!";
            moneyGainedText.text = "GAINED $" + moneyGained.ToString("0.00");
            pointsGainedText.text = "POINTS " + points;
            totalBalanceText.text = "BALANCE $" + money.ToString("0.00");
            lostReasonText.text = "";
            
            loseWinContinueButton.onClick.RemoveAllListeners();
            loseWinContinueButton.onClick.AddListener(() => SwitchMenu(2));
        }

        private float NewEnergy(float maxEnergy, string curEnergyText)
        {
            float minValue = 0, maxValue = 0;
            float currentEnergy = float.Parse(curEnergyText);
            if ((currentEnergy - 2) < (maxEnergy / 2))
            {
                minValue = currentEnergy + 2;
            }
            else
            {
                minValue = (currentEnergy - 2);
            }
            
            if ((currentEnergy + 2) > (maxEnergy + overloadBuffer + 2))
            {
                maxValue = (maxEnergy + overloadBuffer + 5);
            }
            else
            {
                maxValue = (currentEnergy + 5);
            }
            
            return Random.Range(minValue, maxValue);
        }
        
        #endregion

        #region Spawning

        void Spawn()
        {
            foreach (var building in buildingItems)
            {
                int buildingInfoIndex = Random.Range(0, possibleBuildings.Length-1);
                building.info = possibleBuildings[buildingInfoIndex];
            }
        }

        #endregion
        
    }

    [System.Serializable]
    public class BuildingItem
    {
        public BuildingInfo info;
        public TextMeshProUGUI buildingName;
        public TextMeshProUGUI buildingEnergyConsumption, maxEnergy, overloadingText;
        public int buildingId;
        public bool isDisabled;
        public Image icon;

        public BuildingItem(BuildingInfo _info, TextMeshProUGUI _buildingName, TextMeshProUGUI _buildingEnergyConsumption, int _buildingId, TextMeshProUGUI _overloadingText, TextMeshProUGUI _maxEnergy)
        {
            info = _info;
            buildingName = _buildingName;
            buildingEnergyConsumption = _buildingEnergyConsumption;
            buildingId = _buildingId;
            overloadingText = _overloadingText;
            maxEnergy = _maxEnergy;
        }
    }
    
    [System.Serializable]
    public class ShopItem
    {
        public int itemId;
        public float price;
        public shopItemType itemType;
        public int intModifier;
        public float floatModifier;

        public string name;
        public string description;
        
        public Button[] buttonsInCategory;

        public ShopItem(int _itemId, float _price, shopItemType _itemType, float _floatModifier, int _intModifier, string _name, string _description, Button[] _buttonsInCategory)
        {
            itemId = _itemId;
            price = _price;
            itemType = _itemType;
            floatModifier = _floatModifier;
            intModifier = _intModifier;
            name = _name;
            description = _description;
            buttonsInCategory = _buttonsInCategory;
        }
    }

    public enum shopItemType
    {
        ptsMult,
        buffInc,
        nrgInc,
        timeDec
    }
}