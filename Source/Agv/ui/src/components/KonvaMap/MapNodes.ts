import Konva from 'konva'
import { Vector2d } from 'konva/lib/types'

/**
 * AGV Graph dimensions definitions
 */
export class GraphDimensions {
  static scale = 1
  static offsetX = 0
  static offsetY = 0
}

/**
 * Edge
 */
export class Edge {
  line: Konva.Line

  constructor(id: string, position1: Vector2d, position2: Vector2d) {
    const x1 = position1.x * GraphDimensions.scale + GraphDimensions.offsetX
    const y1 = position1.y * GraphDimensions.scale + GraphDimensions.offsetY
    const x2 = position2.x * GraphDimensions.scale + GraphDimensions.offsetX
    const y2 = position2.y * GraphDimensions.scale + GraphDimensions.offsetY

    this.line = new Konva.Line({
      id: id,
      points: [x1, y1, x2, y2],
      stroke: 'darkgray',
      strokeWidth: 3,
      tension: 0
    })
  }

  rescale(oldScale: number, oldOffsetX: number, oldOffsetY: number): void {
    if (oldScale === 0) {
      oldScale = 1 // Avoid 'division by zero' error
    }

    const points = this.line.points()

    // Reset scale
    points[0] = (points[0] - oldOffsetX) / oldScale
    points[1] = (points[1] - oldOffsetY) / oldScale
    points[2] = (points[2] - oldOffsetX) / oldScale
    points[3] = (points[3] - oldOffsetY) / oldScale

    // Set new scale
    points[0] = points[0] * GraphDimensions.scale + GraphDimensions.offsetX
    points[1] = points[1] * GraphDimensions.scale + GraphDimensions.offsetY
    points[2] = points[2] * GraphDimensions.scale + GraphDimensions.offsetX
    points[3] = points[3] * GraphDimensions.scale + GraphDimensions.offsetY

    this.line.points(points)
  }
}

/**
 * Base class for nodes
 */
export abstract class MapNode {
  icon: Konva.Shape
  tag: Konva.Text

  protected tagOffsetY = -32 // Experimentally determined

  constructor(id: string, position: Vector2d) {
    const x = position.x * GraphDimensions.scale + GraphDimensions.offsetX
    const y = position.y * GraphDimensions.scale + GraphDimensions.offsetY

    this.tag = new Konva.Text({
      text: id
    })

    this.icon = this.initIcon()

    this.tag.position({
      x: x - this.tag.width() / 2,
      y: y + this.tagOffsetY
    })

    this.icon.position({ x, y })
  }

  abstract initIcon(): Konva.Shape

  getId(): string {
    return this.tag.text()
  }

  getPosition(): Vector2d {
    return this.icon.position()
  }

  update(position: Vector2d, rotation: number): void {
    const x = position.x * GraphDimensions.scale + GraphDimensions.offsetX
    const y = position.y * GraphDimensions.scale + GraphDimensions.offsetY

    this.tag.position({
      x: x - this.tag.width() / 2,
      y: y + this.tagOffsetY
    })
    this.icon.position({ x, y })
    this.icon.rotation(rotation)
  }

  rescale(oldScale: number, oldOffsetX: number, oldOffsetY: number): void {
    if (oldScale === 0) {
      oldScale = 1 // Avoid 'division by zero' error
    }

    const position = this.icon.position()

    // Reset scale
    position.x = (position.x - oldOffsetX) / oldScale
    position.y = (position.y - oldOffsetY) / oldScale

    // Set new scale
    position.x = position.x * GraphDimensions.scale + GraphDimensions.offsetX
    position.y = position.y * GraphDimensions.scale + GraphDimensions.offsetY

    this.icon.position(position)

    this.tag.position({
      x: position.x - this.tag.width() / 2,
      y: position.y + this.tagOffsetY
    })
  }
}

/**
 * Waypoint node
 */
export class WayPoint extends MapNode {
  initIcon(): Konva.Shape {
    this.tagOffsetY = -16 // Experimentally determined
    return new Konva.Circle({
      radius: 3,
      fill: 'black'
    })
  }
}

/**
 * Station node (Source or Destination)
 */
export class Station extends MapNode {
  initIcon(): Konva.Shape {
    return new Konva.RegularPolygon({
      rotation: 180 / 4, // fix oriantaion
      sides: 4,
      radius: 20,
      fill: 'gray',
      stroke: 'lightgrey',
      strokeWidth: 1
    })
  }
}

/**
 * Parking node
 */
export class Park extends MapNode {
  initIcon(): Konva.Shape {
    return new Konva.RegularPolygon({
      rotation: 180 / 8, // fix oriantaion
      sides: 8,
      radius: 20,
      fill: 'lightgreen',
      stroke: 'black',
      strokeWidth: 1
    })
  }
}

/**
 * Charging node
 */
export class Charge extends MapNode {
  initIcon(): Konva.Shape {
    return new Konva.RegularPolygon({
      rotation: 180 / 8, // fix oriantaion
      sides: 8,
      radius: 20,
      fill: 'yellow',
      stroke: 'black',
      strokeWidth: 1
    })
  }
}
