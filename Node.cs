using System;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
  public int id;
  public int layer;
  public float inputValue;
  public float outputValue;
  public List<Connection> outputConnections;

  public Node(int idNumber)
  {
    this.id = idNumber;
    this.layer = 0; // What layer the node is on, input layer is 0, output layer is 1
    this.inputValue = 0f; // Current sum before activation
    this.outputValue = 0f; // After activation is applied
    this.outputConnections = new List<Connection>(); // Stores all outgoing connections from a node
  }

  // Processes the node's input value using an activation function
  // and then propagates the result to the connected nodes
  public void Engage()
  {
    // If the layer is not the input layer, i.e., hidden or output, then use the activation function, sigmoid
    if (this.layer != 0)
    {
      this.outputValue = Tanh(this.inputValue);
    }

    // Loop over all output connections from this node and propagate the values to the connected node's input
    foreach (var connection in this.outputConnections)
    {
      if (connection.enabled)
      {
        connection.toNode.inputValue += connection.weight * this.outputValue;
      }
    }
  }

  private float Sigmoid(float x)
  {
    return 1.0f / (1.0f + Mathf.Exp(-4.9f * x));
  }

  private float Tanh(float x) {
    return (float)Math.Tanh(x);
  }


  public Node Clone()
  {
    Node clone = new Node(this.id);
    clone.layer = this.layer;
    return clone;
  }

  // Checks if there is a connection between this node and another specified node.
  // This function is particularly useful when trying to add a new connection
  // to ensure that redundant or cyclic connections are not created.
  public bool IsConnectedTo(Node node)
  {
    // Nodes on the same layer will not be connected
    if (node.layer == this.layer)
    {
      return false;
    }

    // If the other node is on a lower layer, we check its output connections to see if it connects to this node
    if (node.layer < this.layer)
    {
      foreach (var connection in node.outputConnections)
      {
        if (connection.toNode == this)
        {
          return true;
        }
      }
    }
    else
    {
      // If this node is on a lower layer, then check this node's connections to see if it connects to the other node
      foreach (var connection in this.outputConnections)
      {
        if (connection.toNode == node)
        {
          return true;
        }
      }
    }

    return false;
  }
}
