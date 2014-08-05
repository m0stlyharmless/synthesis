﻿using System;
using System.Collections.Generic;
using System.IO;

public class BXDJSkeleton
{
    /// <summary>
    /// Ensures that every node is assigned a model file name by assigning all nodes without a file name a generate name.
    /// </summary>
    /// <param name="baseNode">The base node of the skeleton</param>
    public static void SetupFileNames(RigidNode_Base baseNode)
    {
        List<RigidNode_Base> nodes = new List<RigidNode_Base>();
        baseNode.ListAllNodes(nodes);
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].modelFileName == null)
            {
                nodes[i].modelFileName = ("node_" + i + ".bxda");
            }
        }
    }

    /// <summary>
    /// Writes out the skeleton file for the skeleton with the base provided to the path provided.
    /// </summary>
    /// <param name="path">The output file path</param>
    /// <param name="baseNode">The base node of the skeleton</param>
    public static void WriteSkeleton(String path, RigidNode_Base baseNode)
    {
        List<RigidNode_Base> nodes = new List<RigidNode_Base>();
        baseNode.ListAllNodes(nodes);

        // Determine the parent and driver IDs for each node in the list.
        int[] parentID = new int[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].GetParent() != null)
            {
                parentID[i] = nodes.IndexOf(nodes[i].GetParent());
                if (parentID[i] < 0)
                {
                    throw new Exception("Can't resolve parent ID for " + nodes[i].ToString());
                }
            }
            else
            {
                parentID[i] = -1;
            }
        }

        // Begin IO
        BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create));

        writer.Write(BXDIO.FORMAT_VERSION);

        // Write node values
        writer.Write(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            writer.Write(parentID[i]);
            nodes[i].modelFileName = (FileUtilities.SanatizeFileName("node_" + i + ".bxda"));

            writer.Write(nodes[i].modelFileName);
            writer.Write(nodes[i].GetModelID());
            if (parentID[i] >= 0)
            {
                nodes[i].GetSkeletalJoint().WriteJoint(writer);
            }
        }
        writer.Close();
    }

    /// <summary>
    /// Reads the skeleton contained in the BXDJ file specified and returns the root node for that skeleton.
    /// </summary>
    /// <param name="path">The input BXDJ file</param>
    /// <returns>The root node of the skeleton</returns>
    public static RigidNode_Base ReadSkeleton(string path)
    {
        BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open));
        try
        {
            // Sanity check
            uint version = reader.ReadUInt32();
            if (version != BXDIO.FORMAT_VERSION)
            {
                throw new Exception("\"" + path + "\" was created with format version " + BXDIO.VersionToString(version) + ", this library was compiled to read version " + BXDIO.VersionToString(BXDIO.FORMAT_VERSION));
            }

            int nodeCount = reader.ReadInt32();
            if (nodeCount <= 0)
            {
                throw new Exception("This appears to be an empty skeleton");
            }
            RigidNode_Base root = null;
            RigidNode_Base[] nodes = new RigidNode_Base[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                nodes[i] = RigidNode_Base.NODE_FACTORY();
                int parent = reader.ReadInt32();
                nodes[i].modelFileName = (reader.ReadString());
                nodes[i].modelFullID = (reader.ReadString());
                if (parent != -1)
                {
                    SkeletalJoint_Base joint = SkeletalJoint_Base.ReadJointFully(reader);
                    nodes[parent].AddChild(joint, nodes[i]);
                }
                else
                {
                    root = nodes[i];
                }
            }

            if (root == null)
            {
                reader.Close();
                throw new Exception("This skeleton has no known base.  \"" + path + "\" is probably corrupted.");
            }
            reader.Close();
            return root;
        }
        catch (Exception e)
        {
            reader.Close();
            throw e.GetBaseException();
        }
    }
}
