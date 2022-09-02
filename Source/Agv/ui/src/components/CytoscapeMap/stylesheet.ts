import { INodeType } from '@/services/api/data-contracts'
import { Stylesheet } from 'cytoscape'

// See https://js.cytoscape.org/#style
export const stylesheet = (): Stylesheet[] => [
  {
    selector: 'node',
    style: {
      'background-color': 'lightblue',
      label: 'data(id)',
      'font-size': '10px'
    }
  },
  {
    selector: `node[ nodeType = "${INodeType.WayPoint}"]`,
    style: {
      'background-color': 'black',
      label: 'data(id)',
      'font-size': '10px',
      width: '5',
      height: '5'
    }
  },
  {
    selector: `node[ nodeType = "${INodeType.Station}"]`,
    style: {
      'background-color': 'lightgrey',
      shape: 'rectangle',
      'border-width': '1px'
    }
  },
  {
    selector: `node[ nodeType = "${INodeType.Park}"]`,
    style: {
      'background-color': 'lightgreen',
      shape: 'octagon',
      'border-width': '1px'
    }
  },
  {
    selector: `node[ nodeType = "${INodeType.Charge}"]`,
    style: {
      'background-color': 'yellow',
      shape: 'octagon',
      'border-width': '1px'
    }
  },
  {
    selector: 'edge',
    style: {
      width: 2,
      'line-color': 'darkgray',
      'target-arrow-color': '#ccc'
    }
  }
]
