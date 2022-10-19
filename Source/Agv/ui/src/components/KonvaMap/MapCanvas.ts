import Konva from 'konva'
import {
  IGraph,
  IAgvPositionStatus,
  IEdge,
  INode,
  INodeType
} from '@/services/api/data-contracts'
import {
  MapNode,
  Charge,
  Edge,
  Park,
  Station,
  WayPoint,
  GraphDimensions
} from './MapNodes'
import { Agv } from './MapAgv'

/**
 * Konva cavas for AGV Graph
 */
export class MapCanvas {
  imageLayer: Konva.Layer
  edgeLayer: Konva.Layer
  nodeLayer: Konva.Layer
  agvLayer: Konva.Layer

  private stage: Konva.Stage

  private _nodes: Map<string, MapNode> = new Map()
  private _edges: Map<string, Edge> = new Map()
  private _agvs: Map<string, Agv> = new Map()
  private _visibleAgvs: string[] = []

  private _background: HTMLImageElement | null = null

  private readonly ZoomFactor = 1.1 // change zoom by 10% for each scroll step
  private readonly MinZoomLevel = 0.5
  private readonly MaxZoomLevel = 10

  /**
   * MapCanvas Constructor
   * @param container: container where the map is rendered in
   */
  constructor(container: HTMLElement) {
    this.stage = new Konva.Stage({
      container: container.id,
      width: container.offsetWidth,
      height: container.offsetHeight,
      draggable: true
    })

    this.imageLayer = new Konva.Layer()
    this.edgeLayer = new Konva.Layer()
    this.nodeLayer = new Konva.Layer()
    this.agvLayer = new Konva.Layer()

    this.stage.add(this.imageLayer)
    this.stage.add(this.edgeLayer)
    this.stage.add(this.nodeLayer)
    this.stage.add(this.agvLayer)

    // Event handler for resizing stage on window resize
    window.addEventListener('resize', () => {
      if (container) {
        this.stage.width(container.offsetWidth)
        this.stage.height(container.offsetHeight)
      }
    })

    // Event handler for zooming
    this.stage.on('wheel', (e) => {
      e.evt.preventDefault() // stop default scrolling

      const oldScale = this.stage.scaleX()
      const pointer = this.stage.getPointerPosition()
      if (pointer) {
        let direction = e.evt.deltaY // zoom direction

        // when using a trackpad, e.evt.ctrlKey is true, in that case invert direction
        if (e.evt.ctrlKey) {
          direction = -direction
        }

        let newScale = oldScale
        if (direction < 0) {
          newScale = oldScale * this.ZoomFactor
          if (newScale > this.MaxZoomLevel) newScale = this.MaxZoomLevel
        } else {
          newScale = oldScale / this.ZoomFactor
          if (newScale < this.MinZoomLevel) newScale = this.MinZoomLevel
        }

        const mousePointTo = {
          x: (pointer.x - this.stage.x()) / oldScale,
          y: (pointer.y - this.stage.y()) / oldScale
        }

        this.stage.scale({ x: newScale, y: newScale })
        this.stage.position({
          x: pointer.x - mousePointTo.x * newScale,
          y: pointer.y - mousePointTo.y * newScale
        })
      }
    })
  }

  /**
   * Add AGV Graphs
   * All graphs will be merged into one to avoid duplicate nodes
   * @param graphs: Graphs
   */
  addGraphs(graphs: IGraph[]): void {
    for (const graph of graphs) {
      const nodes = graph.nodes
      if (nodes != null) {
        this._addNodes(nodes)
      }
      const edges = graph.edges
      if (edges != null) {
        this._addEdges(edges)
      }
    }
  }

  _addNodes(nodes: INode[]): void {
    for (const node of nodes) {
      if (!this._nodes.has(node.id)) {
        let newNode: MapNode | null = null

        switch (node.type) {
          case INodeType.Charge:
            newNode = new Charge(node.id, { x: node.x, y: node.y })
            break

          case INodeType.WayPoint:
            newNode = new WayPoint(node.id, { x: node.x, y: node.y })
            break

          case INodeType.Turn:
            newNode = new WayPoint(node.id, { x: node.x, y: node.y })
            break

          case INodeType.Station:
            newNode = new Station(node.id, { x: node.x, y: node.y })
            break

          case INodeType.Park:
            newNode = new Park(node.id, { x: node.x, y: node.y })
            break
        }

        if (newNode) {
          this._nodes.set(node.id, newNode)
          this.nodeLayer.add(newNode.tag)
          this.nodeLayer.add(newNode.icon)
        }
      }
    }
  }

  _addEdges(edges: IEdge[]): void {
    for (const edge of edges) {
      const edgeId = `${edge.from.id}-${edge.to.id}`

      if (!this._edges.has(edgeId)) {
        const posFrom = { x: edge.from.x, y: edge.from.y }
        const posTo = { x: edge.to.x, y: edge.to.y }

        const newEdge = new Edge(edgeId, posFrom, posTo)
        this._edges.set(edgeId, newEdge)
        this.edgeLayer.add(newEdge.line)
      }
    }
  }

  /**
   * Sets which AGVs are visible
   * @param agvIds: IDs of visible AGVs
   */
  setVisibleAgvs(agvCorrelationIds: string[]): void {
    this._visibleAgvs = agvCorrelationIds

    for (const agv of this._agvs.values()) {
      const visible = this._visibleAgvs.includes(agv.getCorrelationId())
      agv.icon.visible(visible)
      agv.tag.visible(visible)
    }
  }

  /**
   * Add AGV positions
   * @param agvs: Agv Positions
   */
  addAgvs(agvs: IAgvPositionStatus[]): void {
    for (const agv of agvs) {
      this.updateAgvPosition(agv)
    }
  }

  /**
   * Updates position of an AGV
   * @param agv: Agv Position
   */
  updateAgvPosition(agv: IAgvPositionStatus): void {
    const rotation = (agv.agvPosition.theta * 180) / Math.PI // Convert radians to degree: [-PI, PI] -> [-180°, 180°]

    let agvNode = this._agvs.get(agv.agvId)

    if (agvNode) {
      agvNode.updateSmooth(
        { x: agv.agvPosition.x, y: agv.agvPosition.y },
        rotation
      )
    } else {
      agvNode = new Agv(
        agv.correlationId,
        agv.agvId,
        agv.agvPosition.x,
        agv.agvPosition.y,
        rotation
      )

      this._agvs.set(agv.agvId, agvNode)
      this.agvLayer.add(agvNode.icon)
      this.agvLayer.add(agvNode.tag)
    }

    const visible = this._visibleAgvs.includes(agv.correlationId)
    agvNode.icon.visible(visible)
    agvNode.tag.visible(visible)
  }

  /**
   * Set background properties
   * Call renderBackground to render
   * @param background
   * @param resolution
   */
  setBackground(
    background: HTMLImageElement,
    resolution: number,
    offsetX: number,
    offsetY: number
  ): void {
    this._background = background

    GraphDimensions.scale = 1 / resolution // Inverse of resolution
    GraphDimensions.offsetX = offsetX / resolution
    GraphDimensions.offsetY = offsetY / resolution

    const image = new Konva.Image({
      image: this._background
    })
    this.imageLayer.destroyChildren()
    this.imageLayer.add(image)
  }

  /**
   * Remove all nodes and edges
   */
  clearGraph(): void {
    this._nodes.clear()
    this._edges.clear()
    this.edgeLayer.destroyChildren()
    this.nodeLayer.destroyChildren()
  }
}
