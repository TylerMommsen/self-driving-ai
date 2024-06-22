using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    private int popSize = 80;
    public int aliveCounter = 80;
    private int genCounter = 1;
    public TextMeshProUGUI genCounterUI;
    public TextMeshProUGUI aliveCounterUI;

    public Population population;

    public NetworkVisualizer networkVisualizer;
    public GameObject tempPopObject;

    void Awake() {
        population.InitializePopulation(popSize);
    }

    void Start()
    {
        networkVisualizer.BuildNetwork(population.population[0].brain);
    }

    void FixedUpdate()
    {
        if (population.gameObject.transform.childCount < popSize || tempPopObject.transform.childCount > 0) return;

        if (population.AllDead() == false) {
            population.UpdatePlayers();
        } else {
            population.NaturalSelection();
            genCounter++;
            UpdateGen();
            aliveCounter = popSize;
            UpdateAlive();
            networkVisualizer.BuildNetwork(population.bestPlayerBrain);
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.D)) {
            Time.timeScale = 3;
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            Time.timeScale = 2;
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            Time.timeScale = 1;
        }
    }

    void UpdateGen() {
        genCounterUI.text = "Generation: " + genCounter.ToString();
    }

    public void UpdateAlive() {
        aliveCounterUI.text = "Alive: " + aliveCounter.ToString();
    }
}
