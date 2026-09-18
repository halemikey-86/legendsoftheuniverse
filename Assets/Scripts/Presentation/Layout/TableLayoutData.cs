using System;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [Serializable]
    public sealed class TableLayoutData
    {
        public float matScaleX = 7f;
        public float matScaleY = 1f;
        public float matScaleZ = 4f;
        public float matPosX;
        public float matPosY;
        public float matPosZ;

        public float cameraHeight = 14f;
        public float orthographicSize = 20f;
        public float cameraZoomMultiplier = 1f;
        public float playmatCameraMargin = 1.15f;

        public float playmatY;
        public float cardY = 0.45f;
        public float handY = 0.42f;

        public float cardScale = 2.6f;
        public float iconScale = 3.8f;
        public float deckCardScale = 2.19f;

        public float supplyX = -20f;
        public float supplyZ = 10.2f;
        public float supplyNumberOffsetX = 2.8f;
        public float supplyNumberOffsetY = 0.22f;

        public float roundNumberOffsetX;
        public float roundNumberOffsetY;
        public float worthNumberOffsetX;
        public float worthNumberOffsetY;
        public float willNumberOffsetX;
        public float willNumberOffsetY;
        public float honorNumberOffsetX;
        public float honorNumberOffsetY;

        public float keptHandSpreadSpacing = 4.26f;
        public float dealHandSpreadSpacing = 4.26f;

        public float deckX = 30.7f;
        public float deckY;
        public float deckZ = -1.68f;

        public float discardX = 19.5f;
        public float discardZ = -9.2f;

        public float iconX = -19.5f;
        public float iconZ = -9.2f;

        public float handCenterX;
        public float handCenterZ = -9.5f;
        public float openingHandCenterX;
        public float openingHandCenterZ = -1.5f;

        public float fieldGridCenterX;
        public float fieldGridCenterZ = 9f;
        public float fieldSlotSpacingX = 5.88f;
        public float fieldSlotSpacingZ = 7.912f;
        public float fieldColliderX = 5.88f;
        public float fieldColliderZ = 7.912f;

        public float relicBondCenterX;
        public float relicBondCenterZ = -1.2f;
        public float fieldSlotSpacing = 5.88f;

        public float roundTrackX = 12.2f;
        public float roundTrackZ = 10.6f;
        public float worthTrackX = 16.8f;
        public float worthTrackZ = 10.6f;
        public float willTrackX = 21.4f;
        public float willTrackZ = 10.6f;
        public float honorX = -13.9f;
        public float honorZ = -0.5f;

        public float stackWellX = 0.5f;
        public float stackWellZ = 6.8f;
        public float willwellX = -12f;
        public float willwellZ = 3.6f;
        public float holdPlateX = -10f;
        public float holdPlateZ = -5.2f;

        public float tableFitMargin = 3.25f;

        public SceneTransformRecord[] sceneTransforms = System.Array.Empty<SceneTransformRecord>();

        public static TableLayoutData CreateDefaults() => new TableLayoutData();

        public Vector3 MatPosition => new(matPosX, matPosY, matPosZ);
        public Vector3 MatScale => new(matScaleX, matScaleY, matScaleZ);
        public float MatWidth => matScaleX * 10f;
        public float MatDepth => matScaleZ * 10f;

        public Vector3 Supply => new(supplyX, cardY, supplyZ);
        public Vector3 SupplyNumberOffset => new(supplyNumberOffsetX, supplyNumberOffsetY, 0f);
        public Vector3 Deck => new(deckX, deckY, deckZ);
        public Vector3 Discard => new(discardX, cardY, discardZ);
        public Vector3 Icon => new(iconX, cardY, iconZ);
        public Vector3 HandCenter => new(handCenterX, handY, handCenterZ);
        public Vector3 OpeningHandCenter => new(openingHandCenterX, handY, openingHandCenterZ);
        public Vector3 FieldDropGridCenter => new(fieldGridCenterX, cardY, fieldGridCenterZ);
        public Vector2 FieldDropSlotSpacing => new(fieldSlotSpacingX, fieldSlotSpacingZ);
        public Vector3 FieldDropColliderSize => new(fieldColliderX, 0.04f, fieldColliderZ);
        public Vector3 RelicBondCenter => new(relicBondCenterX, cardY, relicBondCenterZ);
        public Vector3 RoundTrack => new(roundTrackX, cardY, roundTrackZ);
        public Vector3 WorthTrack => new(worthTrackX, cardY, worthTrackZ);
        public Vector3 WillTrack => new(willTrackX, cardY, willTrackZ);
        public Vector3 Honor => new(honorX, cardY, honorZ);
        public Vector3 StackWell => new(stackWellX, cardY, stackWellZ);
        public Vector3 Willwell => new(willwellX, cardY, willwellZ);
        public Vector3 HoldPlate => new(holdPlateX, cardY, holdPlateZ);
    }
}
