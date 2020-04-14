import React from 'react';

class Content extends React.Component {

    render() {
        return (
            <div>
                <div>Token: {this.props.apiToken}</div>
                <div><a href="#" onClick={this.props.logout}>Logout</a></div>
                <p>
                    Machine Temperature {this.props.machine.temperature}
                </p>
                <p>
                    Machine Pressure: {this.props.machine.pressure}
                </p>
                <p>
                    Ambient Temperature: {this.props.ambient.temperature}
                </p>
                <p>
                    Ambient Humidity: {this.props.ambient.humidity}
                </p>
                <p>
                    Measurement Date: {this.props.measurementDate}
                </p>
            </div>
        )
    }
    
}

export default Content;