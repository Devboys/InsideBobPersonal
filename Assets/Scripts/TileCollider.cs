﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCollider : MonoBehaviour
{

    public TileBase diseaseTile;
    public TileBase bacteriaTile;
    public TileBase walkingTile;

    private PlayerController playerController;
    private LevelController levelController;

    public GameObject diseaseClearParticlePrefab;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        levelController = FindObjectOfType<LevelController>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Tilemap tilemap = collision.gameObject.GetComponent<Tilemap>();
        if (tilemap)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector3Int tilePosition = tilemap.WorldToCell(contact.point - contact.normal * 0.01f);
                TileBase tile = tilemap.GetTile(tilePosition);
                if (tile == diseaseTile)
                {
                    tilemap.SetTile(tilePosition, walkingTile);
                    levelController.CheckOpen();

                    //instantiate particle prefab at player's feet.
                    var particle = Instantiate(diseaseClearParticlePrefab);
                    //Vector3 pos = tilemap.CellToWorld(tilePosition);
                    //pos.z = particle.transform.position.z;
                    Vector3 pos = contact.point - contact.normal * 0.01f;
                    Quaternion rot = Quaternion.identity;

                    //if clearing from below tile, send particles downwards
                    if ((Mathf.Abs(this.transform.position.y) - Mathf.Abs(pos.y) > 0))
                    {
                        rot = Quaternion.LookRotation(Vector2.down);
                    }
                    particle.transform.SetPositionAndRotation(pos, rot);


                    //TODO: SOUND - PLAY DISEASE TILE REMOVE SOUND HERE

                }
                else if (tile == bacteriaTile)
                {
                    playerController.Die();
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PillHandler pill = collision.gameObject.GetComponent<PillHandler>();
        if (pill) {
            pill.gameObject.SetActive(false);
            playerController.totalPillsPickedUp++;
            levelController.PillTaken();
            return;
        }

        PowerupHandler powerup = collision.gameObject.GetComponent<PowerupHandler>();
        if (powerup)
        {
            powerup.gameObject.SetActive(false);
            playerController.AddPlatform(powerup.padsGiven);
            return;
        }
    }
}