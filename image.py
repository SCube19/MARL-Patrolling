import cv2
import numpy as np
import os
import copy 


for root, _, files in os.walk('.'):
    for file in files:
        name = os.path.join(root, file)
        if name.endswith('.meta'):
            os.remove(name)

def blend(directory, background, strength): # Blend images equally.
    composite = None

    subtractor = cv2.createBackgroundSubtractorMOG2()

    result = background

    for path in os.listdir(directory):
        if not path.endswith('.png'):
            continue

        img = cv2.imread(f'{directory}/{path}', cv2.IMREAD_UNCHANGED)
        mask = subtractor.apply(img)

        ret, th1 = cv2.threshold(mask, 15, strength, cv2.THRESH_BINARY)

        if composite is None:
            composite = np.zeros((img.shape[0], img.shape[1]), np.uint8)
        composite = cv2.add(composite, th1)

        heatmap = cv2.applyColorMap(composite, cv2.COLORMAP_JET)
        result = cv2.addWeighted(background, 0.5, heatmap, 0.5, 0)

    return result

def blendDirectory(directory):
    background = cv2.imread(f'{directory}/background.png')

    guardSpheres = blend(f'{directory}/spheres/guard', background, 10)
    thiefSpheres = blend(f'{directory}/spheres/thief', background, 10)
    guardTrails = blend(f'{directory}/trails/guard', background, 2)
    thiefTrails = blend(f'{directory}/trails/thief', background, 2)

    cv2.imwrite(f'{directory}/g-sphere.png', guardSpheres)
    cv2.imwrite(f'{directory}/t-sphere.png', thiefSpheres)
    cv2.imwrite(f'{directory}/g-trail.png', guardTrails)
    cv2.imwrite(f'{directory}/t-trail.png', thiefTrails)

# directories = os.listdir('.')
# for directory in directories:
#     blendDirectory(directory)
blendDirectory('gen3shots')